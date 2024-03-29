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

## Links

Description | Link
---------|----------
 This repository | [embergershared/c-sharp-k8s-client](https://github.com/embergershared/c-sharp-k8s-client)
 The C# Kubernetes client GitHub repo | [C# .NET Kubernetes client](https://github.com/kubernetes-client/csharp)
 Microsoft Learn ASP.NET Core WebAPI Overview | [ASP.NET Core in .NET 8.0 C# WebAPI](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-8.0)
 Bridge to Kubernetes tool to redirect an AKS service to a local dev machine for debug purposes | [Bridge to Kubernetes overview](https://learn.microsoft.com/en-us/visualstudio/bridge/overview-bridge-to-kubernetes)
 Azure Service Bus client library for .NET | [Azure Service Bus client library for .NET](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.servicebus-readme?view=azure-dotnet)
 Service Bus technical documentation | [What is Azure Service Bus?](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview)
 Service Bus queues quickstarts .NET | [Send and receive messages from an Azure Service Bus queue](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?tabs=passwordless)
 Dependency injection with the Azure SDK for .NET | [Dependency injection with the Azure SDK for .NET](https://learn.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection?tabs=web-app-builder)
