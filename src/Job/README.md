# **Job** Visual Studio 2022 Project

## Overview

## Update/Push image to Container Registry

```powershell
az acr login --name $ACR
cd Job/
docker build -t jobworker:dev -f Dockerfile .
docker tag jobworker:dev "$ACR.azurecr.io/bases-jet/jobworker:dev"
docker push "$ACR.azurecr.io/bases-jet/jobworker:dev"
```
