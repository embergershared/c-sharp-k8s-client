# Kubernetes C# client example used within the AKS cluster

## Overview

This repository ([c-sharp-k8s-client](https://github.com/embergershared/c-sharp-k8s-client)) uses the [C# .NET Kubernetes client](https://github.com/kubernetes-client/csharp) within an [ASP.NET Core in .NET 8.0 C# WebAPI](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-8.0), as a NuGet Package (`KubernetesClient`), called `ListenerAPI` to access, manipulate and administer Kubernetes objects while running in the cluster itself.

Specifically, the Listener exposes 3 WebAPIs to:

1. List `namespaces`,
2. List `pods`,
3. Create `jobs`, providing their name (but can easily be expanded to a `JSON` definition payload).

The kubernetes jobs are created using the image `jobworker:dev` to perform tasks, then complete.

The code and images are organized in this manner:

Role | Project Name | [.NET template](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview) | Visual Studio Project | Docker Image Name
---------|----------|---------|---------|---------
 WebAPI Listener | `ListenerAPI` | ASP.NET Core Web API | `src/ListenerAPI/ListenerAPI.csproj` | `listenerapi:dev`
 Job Worker | `Job` | Worker Service | `src/Job/Job.csproj` | `jobworker:dev`

## Figure

The process is represented by this UML Sequence diagram to create `jobs`:

![UML sequence](img/POC_UML.jpg)

## Azure Service Bus Messages trigger

The above feature works with a `HTTP(S) POST` on `api/Jobs`(the **Job controller**) with a job name parameter.
It then creates a Kubernetes job: a pod built with the job image and run it until completed.

An additional requirement was added to trigger a job from a `received Azure Service Bus Message`.
To do that, the following has been added:

- a `api/Messages` Controller to Create and Delete Service Bus Messages,
- a `SbProcessor` Background task that listens to messages. It could then create Jobs based on the message (code is not implemented), and it can be turned `ON`/`OFF`.

### Setup

0. Set few variables values

```powershell
$RESOURCE_GROUP=""
$LOCATION=""
$SUBSCRIPTION="$(az account show --query id --output tsv)"
$USER_ASSIGNED_IDENTITY_NAME="uai-listener-asb"
$SERVICE_ACCOUNT_NAMESPACE="bases-jet"
$SERVICE_ACCOUNT_NAME="listener-service-account"
$FEDERATED_IDENTITY_CREDENTIAL_NAME="listenerFedIdentity" 
$AKS_CLUSTER_NAME=""
$SERVICE_BUS_QUEUE_ID=""
```

For the `ListenerAPI` to securely access the Service Bus, we use the [`Workload Identity feature`](https://learn.microsoft.com/en-us/azure/aks/workload-identity-deploy-cluster). The following setup instructions are inspired by this tutorial: [Use a workload identity with an application on Azure Kubernetes Service (AKS)](https://learn.microsoft.com/en-us/azure/aks/learn/tutorial-kubernetes-workload-identity). Here are the steps to follow:

1. Enable `Workload Identity` and `OIDC` on AKS

```powershell
az aks update -g $RESOURCE_GROUP -n $AKS_CLUSTER_NAME --enable-workload-identity --enable-oidc-issuer
```

> Store the OIDC issuer URL:

```powershell
$AKS_OIDC_ISSUER="$(az aks show -n $AKS_CLUSTER_NAME -g $RESOURCE_GROUP --query "oidcIssuerProfile.issuerUrl" -otsv)"
```

2. Create a [`Managed Identity`](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/overview#managed-identity-types) of type `User-assigned`. This is the Azure Identity the `ListenerAPI` will use to access the Service Bus Queue

```powershell
az identity create --name $USER_ASSIGNED_IDENTITY_NAME --resource-group $RESOURCE_GROUP --location $LOCATION --subscription $SUBSCRIPTION
```

> Store the User-assigned Identity client ID:

```powershell
$USER_ASSIGNED_CLIENT_ID="$(az identity show -g $RESOURCE_GROUP -n $USER_ASSIGNED_IDENTITY_NAME --query 'clientId' -otsv)"
```

3. Create 2 role assignments for the ListenerAPI's User-assigned Identity to access the Service Bus & Manage the queue

```powershell
$SERVICE_BUS_ID = $SERVICE_BUS_QUEUE_ID -split '/queues', 2 | Select-Object -First 1
az role assignment create --assignee $USER_ASSIGNED_CLIENT_ID --role "Reader" --scope $SERVICE_BUS_ID # Allows the ListenerAPI to access the Service Bus
az role assignment create --assignee $USER_ASSIGNED_CLIENT_ID --role "Azure Service Bus Data Owner" --scope $SERVICE_BUS_QUEUE_ID # Allows the ListenerAPI to create and receive messages in this queue
```

4. Update the Helm chart `values.yaml`

Set the value for `listener.serviceBus.listenerUaiClientId` with the `$USER_ASSIGNED_CLIENT_ID`.

5. Deploy the updated Helm chart, to create the service account and update the `ListenerAPI` pod

```powershell
helm upgrade listener ./helm-chart --namespace $SERVICE_ACCOUNT_NAMESPACE --values ./helm-chart/values-secret.yaml
```

6. Create a Federated credential to link the `ListenerAPI` Kubernetes service account with the Azure User-assigned Identity

```powershell
az identity federated-credential create --name $FEDERATED_IDENTITY_CREDENTIAL_NAME --identity-name $USER_ASSIGNED_IDENTITY_NAME --resource-group $RESOURCE_GROUP --issuer $AKS_OIDC_ISSUER --subject system:serviceaccount:${SERVICE_ACCOUNT_NAMESPACE}:${SERVICE_ACCOUNT_NAME} --audience api://AzureADTokenExchange
```

7. Test

- Create messages:

```powershell
$body = @{
    jobName = "test-job"
    messagesToCreateCount = 15
} | ConvertTo-Json
$createResponse = Invoke-RestMethod -Uri http://localhost:5269/api/Messages -Method 'POST' -Body $body -ContentType 'application/json'
$createResponse
```

- Delete ALL messages:

```powershell
$delResponse = Invoke-RestMethod -Uri http://localhost:5269/api/Messages -Method 'DELETE'
$delResponse
```

## KEDA Scaler based on Azure Service Bus messages

We can also scale based on the amount of messages present in the Queue.

### Setup

0. Set few variables values

```powershell
$RESOURCE_GROUP=""
$LOCATION=""
$SUBSCRIPTION="$(az account show --query id --output tsv)"
$USER_ASSIGNED_IDENTITY_NAME="uai-keda-asb"
$FEDERATED_IDENTITY_CREDENTIAL_NAME="KedaServiceBusK8sSaFedCred"
$SERVICE_ACCOUNT_NAMESPACE="bases-jet"
$SERVICE_ACCOUNT_NAME="keda-service-account"
$AKS_CLUSTER_NAME=""
$SERVICE_BUS_QUEUE_ID=""
```

1. Enable KEDA add-on

```powershell
az aks update -g $RESOURCE_GROUP -n $AKS_CLUSTER_NAME --enable-keda
```

Control with:

```powershell
# Check add-on installation:
az aks show -g $RESOURCE_GROUP -n $AKS_CLUSTER_NAME --query "workloadAutoScalerProfile.keda.enabled"
# Check KEDA runs:
kubectl get pods -n kube-system
# Check KEDA version:
kubectl get crd/scaledobjects.keda.sh -o yaml
```

2. Enable Workload Identity and OIDC on AKS **(if not done yet for the `ListenerAPI`)**

```powershell
az aks update -g $RESOURCE_GROUP -n $AKS_CLUSTER_NAME --enable-workload-identity --enable-oidc-issuer
```

> Store the OIDC issuer URL: `$AKS_OIDC_ISSUER="$(az aks show -n $AKS_CLUSTER_NAME -g $RESOURCE_GROUP --query "oidcIssuerProfile.issuerUrl" -otsv)"`

3. Create a [`Managed Identity`](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/overview#managed-identity-types) of type `User-assigned`. This identity will be used by KEDA to query the Service Bus

```powershell
az identity create --name $USER_ASSIGNED_IDENTITY_NAME --resource-group $RESOURCE_GROUP --location $LOCATION --subscription $SUBSCRIPTION
```

- Store:
  - the client ID: `$USER_ASSIGNED_CLIENT_ID="$(az identity show -g $RESOURCE_GROUP -n $USER_ASSIGNED_IDENTITY_NAME --query 'clientId' -otsv)"`
  - the tenant ID: `$TENANT_ID="$(az identity show -g $RESOURCE_GROUP -n $USER_ASSIGNED_IDENTITY_NAME --query 'tenantId' -otsv)"`

4. Create a role assignment for the KEDA's User-assigned Identity to read the Service Bus queue

```powershell
az role assignment create --assignee $USER_ASSIGNED_CLIENT_ID --role "Azure Service Bus Data Receiver" --scope $SERVICE_BUS_QUEUE_ID
```

5. Create a Kubernetes Service Account for KEDA in AKS

```powershell
kubectl apply -f src/ListenerAPI/k8s/bases-jet-KEDA-sa.yaml
```

6. Create a Federated credential to link the `KEDA` Kubernetes service account with the Azure User-assigned Identity

```powershell
az identity federated-credential create --name $FEDERATED_IDENTITY_CREDENTIAL_NAME --identity-name $USER_ASSIGNED_IDENTITY_NAME --resource-group $RESOURCE_GROUP --issuer $AKS_OIDC_ISSUER --subject system:serviceaccount:$SERVICE_ACCOUNT_NAMESPACE:$SERVICE_ACCOUNT_NAME --audience api://AzureADTokenExchange
```

7. Create a Federated credential to link the `KEDA Operator` Kubernetes service account with the Azure User-assigned Identity

```powershell
az identity federated-credential create --name kedaOperatorFedIdentity --identity-name $USER_ASSIGNED_IDENTITY_NAME --resource-group $RESOURCE_GROUP --issuer $AKS_OIDC_ISSUER --subject system:serviceaccount:"kube-system":"keda-operator" --audience api://AzureADTokenExchange
```

8. Restart KEDA

```powershell
kubectl rollout restart deployment keda-operator -n kube-system
```

9. Create a scaler on the Service Bus queue

```powershell
kubectl apply -f src/ListenerAPI/k8s/bases-jet-KEDA-asb.yaml
```

10. Check the scaler is `READY`

```powershell
kubectl get ScaledObject
```

11. Create Messages in the Service Bus Queue

- Observe the scaling of the `ListenerAPI` Deployment's pods and the user node pool.

12. Tune AKS Cluster Autoscaler profile (aggressive scale down profile inspired example)

```powershell
az aks update -g $RESOURCE_GROUP -n $AKS_CLUSTER_NAME --cluster-autoscaler-profile scan-interval=15s
az aks update -g $RESOURCE_GROUP -n $AKS_CLUSTER_NAME --cluster-autoscaler-profile scale-down-unneeded-time=1m
az aks update -g $RESOURCE_GROUP -n $AKS_CLUSTER_NAME --cluster-autoscaler-profile scale-down-unready-time=3m
az aks update -g $RESOURCE_GROUP -n $AKS_CLUSTER_NAME --cluster-autoscaler-profile max-graceful-termination-sec=30
az aks update -g $RESOURCE_GROUP -n $AKS_CLUSTER_NAME --cluster-autoscaler-profile skip-nodes-with-local-storage=false
az aks update -g $RESOURCE_GROUP -n $AKS_CLUSTER_NAME --cluster-autoscaler-profile max-empty-bulk-delete=1000
az aks update -g $RESOURCE_GROUP -n $AKS_CLUSTER_NAME --cluster-autoscaler-profile max-total-unready-percentage=100
az aks update -g $RESOURCE_GROUP -n $AKS_CLUSTER_NAME --cluster-autoscaler-profile ok-total-unready-count=1000
az aks update -g $RESOURCE_GROUP -n $AKS_CLUSTER_NAME --cluster-autoscaler-profile max-node-provision-time=15m
```

## Links

Description | Link
---------|----------
 This repository | [embergershared/c-sharp-k8s-client](https://github.com/embergershared/c-sharp-k8s-client)
 The C# Kubernetes client GitHub repo | [C# .NET Kubernetes client](https://github.com/kubernetes-client/csharp)
 Bridge to Kubernetes tool to redirect an AKS service to a local dev machine for debug purposes | [Bridge to Kubernetes overview](https://learn.microsoft.com/en-us/visualstudio/bridge/overview-bridge-to-kubernetes)
 Microsoft Learn ASP.NET Core WebAPI Overview | [ASP.NET Core in .NET 8.0 C# WebAPI](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-8.0)
 Dependency injection with the Azure SDK for .NET | [Dependency injection with the Azure SDK for .NET](https://learn.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection?tabs=web-app-builder)
 Azure Identity Client | [Azure Identity client library for .NET](https://github.com/Azure/azure-sdk-for-net/blob/Azure.Identity_1.11.0/sdk/identity/Azure.Identity/README.md)
 .NET Core `IHostedService` to build Hosted services / background tasks | [Background tasks](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-8.0&tabs=visual-studio)
 Azure Service Bus client library for .NET | [Azure Service Bus client library for .NET](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.servicebus-readme?view=azure-dotnet)
 Azure Service Bus technical documentation | [What is Azure Service Bus?](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview)
 Azure Service Bus queues quickstarts .NET | [Send and receive messages from an Azure Service Bus queue](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?tabs=passwordless)
 Azure Service Bus sample processor | [Service Bus Processor sample](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?tabs=passwordless#add-the-code-to-receive-messages-from-the-queue)
 Azure Service Bus dependency injection | [Registering Service Bus client with ASP.NET Core](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.servicebus-readme?view=azure-dotnet#registering-with-aspnet-core-dependency-injection)
 AKS KEDA add-on | [Install the Kubernetes Event-driven Autoscaling (KEDA) add-on using the Azure CLI](https://learn.microsoft.com/en-us/azure/aks/keda-deploy-add-on-cli#install-the-keda-add-on-with-azure-cli)
 KEDA Scaler for Azure Service Bus | [Azure Service Bus trigger](https://keda.sh/docs/2.13/scalers/azure-service-bus/)
 Azure AD Workload Identity | [Introduction](https://azure.github.io/azure-workload-identity/docs/)
 KEDA Scaling for NET based on Service Bus | [.NET Core worker processing Azure Service Bus Queue scaled by KEDA](https://github.com/kedacore/sample-dotnet-worker-servicebus-queue)
 KEDA sample with AKS Workload Identity | [.NET Core worker processing Azure Service Bus Queue scaled by KEDA with Azure AD Workload Identity](https://github.com/kedacore/sample-dotnet-worker-servicebus-queue/blob/main/workload-identity.md)
 AKS Autoscaler profile | [Use the cluster autoscaler in Azure Kubernetes Service (AKS)](https://learn.microsoft.com/en-us/azure/aks/cluster-autoscaler?tabs=azure-cli#use-the-cluster-autoscaler-profile)
 AKS Workload Identity with an app | [Use a workload identity with an application on Azure Kubernetes Service (AKS)](https://learn.microsoft.com/en-us/azure/aks/learn/tutorial-kubernetes-workload-identity)

## Extras

- To list all Federated Credentials attached to a Managed Identity:

```powershell
az identity federated-credential list -g $RESOURCE_GROUP --identity-name $USER_ASSIGNED_IDENTITY_NAME
```
