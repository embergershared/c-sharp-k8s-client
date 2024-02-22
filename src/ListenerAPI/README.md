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

```powershell
az aks update -n $AKS_NAME -g $AKS_RG --attach-acr $ACR_ID
## Create Namespace
kubetcl apply -f k8s/bases-jet-ns.yaml
## Create Deployment
kubetcl apply -f k8s/bases-jet-dep.yaml
## Create Service
kubetcl apply -f k8s/bases-jet-svc.yaml
```

3. Create permissions for the Listener in the AKS cluster

```powershell
# To list pods and namespaces (used by api/namespaces & api/pods)
kubectl apply -f k8s/bases-jet-clusterRole.yaml
kubectl apply -f k8s/bases-jet-clusterRoleBinding.yaml

# To administer jobs in the namespace (used by api/jobs)
kubectl apply -f k8s/bases-jet-Role.yaml
kubectl apply -f k8s/bases-jet-RoleBinding.yaml
```

4. Use the WebAPI

`$IPPort=""`

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

## Update/Push image to Container Registry

```powershell
az acr login --name $ACR
docker build -t listenerapi:dev -f Dockerfile .
docker tag listenerapi:dev "$ACR.azurecr.io/bases-jet/listenerapi:dev"
docker push "$ACR.azurecr.io/bases-jet/listenerapi:dev"
kubectl delete pod/listener-dep-***-*** # Pods deletion forces a recreation, that will pull the latest image (based on its digest) as we have a `spec.template.spec.containers.imagePullPolicy: Always` parameter.
```
