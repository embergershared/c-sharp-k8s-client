# `Job` Visual Studio 2022 Project

## Overview

This Visual Studio project is a .NET Core `Worker Service`.
It launches an Asynchronous process that logs a message every second, then exits and stops the worker.
The number of iterations is set by an Environment variable `ITERATIONS`.

This image is used in the job definition the `ListenerAPI` uses to create the job (in `src\ListenerAPI\Classes\k8sClient.cs` / `CreateJobAsync()`). In this class, the number of iterations is set to `12`. This can be extended to become an argument from the trigger, and actually many to set the worker's instance details.

## Update/Push image to Container Registry

```powershell
az acr login --name $ACR
docker build -t jobworker:dev -f Dockerfile .
docker tag jobworker:dev "$ACR.azurecr.io/bases-jet/jobworker:dev"
docker push "$ACR.azurecr.io/bases-jet/jobworker:dev"
```
