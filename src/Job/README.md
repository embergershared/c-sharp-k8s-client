# `Job` Visual Studio 2022 Project

## Overview

This Visual Studio project is a .NET Core `Worker Service`.
It launches an Asynchronous process that logs a message every second, then exits and stops the worker.
The number of iterations is set by an Environment variable `ITERATIONS`.

This image is used in the job definition the `ListenerAPI` uses to create the job (in `src\ListenerAPI\Classes\k8sClient.cs` / `CreateJobAsync()`). In this class, the number of iterations is set to `12`. This can be extended to become an argument from the trigger, and actually many to set the worker's instance details.

The outputs of the job can be seen with `kubectl logs pod/<name of the pod>`:
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
      Worker running iteration 3/12 at: 02/21/2024 19:40:35 +00:00
info: Job.Worker[0]
      Worker running iteration 4/12 at: 02/21/2024 19:40:36 +00:00
info: Job.Worker[0]
      Worker running iteration 5/12 at: 02/21/2024 19:40:37 +00:00
info: Job.Worker[0]
      Worker running iteration 6/12 at: 02/21/2024 19:40:38 +00:00
info: Job.Worker[0]
      Worker running iteration 7/12 at: 02/21/2024 19:40:39 +00:00
info: Job.Worker[0]
      Worker running iteration 8/12 at: 02/21/2024 19:40:40 +00:00
info: Job.Worker[0]
      Worker running iteration 9/12 at: 02/21/2024 19:40:41 +00:00
info: Job.Worker[0]
      Worker running iteration 10/12 at: 02/21/2024 19:40:42 +00:00
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
