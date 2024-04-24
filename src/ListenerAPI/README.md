# `ListenerAPI` Visual Studio 2022 Project

## Overview

## Deploy the Listener

1. Build and push the application image to an ACR

```powershell
az acr login --name $ACR
docker build -t listenerapi:dev -f Dockerfile .
docker tag listenerapi:dev "$ACR.azurecr.io/bases-jet/listenerapi:dev"
docker push "$ACR.azurecr.io/bases-jet/listenerapi:dev"
```

2. Authorize AKS to the ACR and deploy the listener

- Authorize the AKS cluster to pull from the ACR

```powershell
az aks update -n $AKS_NAME -g $AKS_RG --attach-acr $ACR_ID
```

- Create a YAML file, not checked-in, for example `dev-secret.yaml` with:

```yaml
image:
  repository: <ACR name>

azureFile:
  storageAccountName: <Storage Account name>
  storageAccountKey: <Storage Account Access key>
  shareName: <Storage Account File share name>
```

- Install the listener helm chart:

```powershell
helm install listener ./helm-chart --namespace bases-jet --create-namespace --values ./helm-chart/dev-secret.yaml
```

3. Use the WebAPI

`$IPPort=$(kubectl get service/listener-svc -n bases-jet -o jsonpath='{.status.loadBalancer.ingress[0].ip}')`

- With Swagger Web UI: `http://$IPPort/swagger/index.html`

- with browser:
  - List namespaces: `http://$IPPort/api/namespaces`
  - List pods: `http://$IPPort/api/pods`
  - Create a job:

  ```bash
  curl -X 'POST' \
  '<http://$IPPort/api/Jobs>' \
  -H 'Content-Type: application/json' \
  -d '"docjob"'
  ```

4. Browse the mounted file share

Connect in the listener pod:

```powershell
$podNames = kubectl get pod -n bases-jet -o jsonpath='{.items[*].metadata.name}'
$array = $podNames -split ' ' | Where-Object { $_ -like "listener-dep*" }
kubectl exec -it $array -- /bin/bash
```

Once in the pod, list the file share content:

`ls -l /mnt`

## Update/Push image to Container Registry

```powershell
az acr login --name $ACR
docker build -t "$ACR.azurecr.io/bases-jet/listenerapi:dev" -f Dockerfile .
# docker tag listenerapi:dev "$ACR.azurecr.io/bases-jet/listenerapi:dev"
docker push "$ACR.azurecr.io/bases-jet/listenerapi:dev"
# The pod deletion forces its re-creation, with the latest image (based on its ACR digest)
# as we have a 'spec.template.spec.containers.imagePullPolicy: Always' parameter in place.
$podNames = kubectl get pod -n bases-jet -o jsonpath='{.items[*].metadata.name}'
$array = $podNames -split ' ' | Where-Object { $_ -like "listener-dep*" }
$array | %{kubectl delete pod $_}
```

## Update the Helm chart

```powershell
helm upgrade listener ./helm-chart
```

## Remove all

```powershell
helm uninstall listener
kubectl delete ns bases-jet
```
