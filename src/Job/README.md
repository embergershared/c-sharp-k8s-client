# `Job` Visual Studio 2022 Project

## Overview

This Visual Studio project is a .NET Core `Worker Service`.
It creates a worked that launches an Asynchronous process. This process logs a message every second (`Information` level), which exits after `X` iterations and stops the worker, making the pod terminating.
The number of iterations `X` is set by the Environment variable `ITERATIONS`'s value.

This image is used in the job definition the `ListenerAPI` creates for the job (in `src\ListenerAPI\Classes\k8sClient.cs` / `CreateJobAsync()`). This can be extended to become an argument from the trigger payload, to become an entire `JSON` that will define the exact parameters of the job's run.

The outputs of the job can be seen with `kubectl logs pod/<name of the pod>`, and here is an example:

```log
info: Job.Worker[0]
      Worker running iteration 1/12 at: 02/21/2024 19:40:33 +00:00
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /app
info: Job.Worker[0]
      Worker running iteration 2/12 at: 02/21/2024 19:40:34 +00:00
info: Job.Worker[0]
.../...
info: Job.Worker[0]
      Worker running iteration 11/12 at: 02/21/2024 19:40:43 +00:00
info: Job.Worker[0]
      Worker running iteration 12/12 at: 02/21/2024 19:40:44 +00:00
info: Job.Worker[0]
      Worker completed 12 iterations at: 02/21/2024 19:40:45 +00:00
info: Microsoft.Hosting.Lifetime[0]
      Application is shutting down...
```

## Update/Push image to Container Registry

```powershell
az acr login --name $ACR
docker build -t jobworker:dev -f Dockerfile .
docker tag jobworker:dev "$ACR.azurecr.io/bases-jet/jobworker:dev"
docker push "$ACR.azurecr.io/bases-jet/jobworker:dev"
```

> Note: because the job created by the `ListenerAPI` has an `ImagePullPolicy: Always` spec, each jon run will update to the latest `dev` tag available in the Container Registry.

## Create and use a dedicated Node pool for jobs

```powershell
$AKS_NAME=""
$AKS_RG=""
$VNET_ID=""
$NODES_SNET_ID="${VNET_ID}/subnets/aks-jobspool-snet"
$PODS_SNET_ID="${VNET_ID}/subnets/aks-pod-snet"

az aks nodepool add -g $AKS_RG -n jobs --cluster-name $AKS_NAME -k 1.27.7 --mode User -c 2 -s Standard_B2s --pod-subnet-id $PODS_SNET_ID --vnet-subnet-id $NODES_SNET_ID
```

It creates an Node pool with nodes getting the agentpool name as a label (`kubernetes.azure.com/agentpool:jobs`)
We can then use it for the Node selector:

```yaml
spec:
  containers:
  nodeSelector:
    kubernetes.azure.com/agentpool: jobs
```
