# Kubernetes C# client example

## Overview

This repository, [c-sharp-k8s-client](https://github.com/embergershared/c-sharp-k8s-client), uses the [C# .NET Kubernetes client](https://github.com/kubernetes-client/csharp) within an [ASP.NET Core in .NET 8.0 C# WebAPI](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-8.0), as a NuGet Package, called `ListenerAPI` to access, manipulate and administer in cluster Kubernetes objects.

Specifically, the Listener exposes its WebAPIs to:

- List `namespaces`
- List `pods`
- Create `jobs`

The jobs are created using the image `jobruntime` to perform few tasks, then complete.

The code and images are organized in this manner:

Role | Application Name | Visual Studio Project | Image Name
---------|----------|---------|---------
 WebAPI Listener | ListenerAPI | `src/ListenerAPI/ListenerAPI.csproj` | `listenerapi:dev`
 Job Runner | Job | `src/Job/Job.csproj` | `jobruntime:dev`


## Links

[c-sharp-k8s-client](https://github.com/embergershared/c-sharp-k8s-client)

[C# .NET Kubernetes client](https://github.com/kubernetes-client/csharp)

[ASP.NET Core in .NET 8.0 C# WebAPI](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-8.0)
