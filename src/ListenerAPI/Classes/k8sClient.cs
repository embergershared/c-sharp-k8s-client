using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using k8s;
using k8s.Autorest;
using k8s.Models;
using ListenerAPI.Constants;
using ListenerAPI.Helpers;
using ListenerAPI.Interfaces;
using ListenerAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ListenerAPI.Classes
{
  public class K8SClient : IK8SClient
  {
    private readonly ILogger<K8SClient> _logger;
    private Kubernetes? _k8SClient;
    private readonly IConfiguration _config;

    public K8SClient(
        ILogger<K8SClient> logger,
        IConfiguration config
        )
    {
      _logger = logger;
      _config = config;
      CreateClient();
    }
    private void CreateClient()
    {
      var kPath = _config.GetValue<string>("kubeconfigPath");
      _logger.LogInformation("Found in config: \"kubeconfigPath\": \"{kPath}\"", kPath);

      // Creating the K8S client:
      // Ref: https://github.com/kubernetes-client/csharp#creating-the-client
      var k8SClientConfiguration = kPath == null ?
          // Load from in-cluster configuration:
          KubernetesClientConfiguration.InClusterConfig() :
          // Load from the default kubeconfig on the machine.
          KubernetesClientConfiguration.BuildConfigFromConfigFile(kPath);

      // Use the config object to create a client.
      try
      {
        _k8SClient = new Kubernetes(k8SClientConfiguration);
      }
      catch (Exception ex)
      {
        _logger.LogError("K8SClient.CreateClient() threw an exception: {ex}", ex.ToString());
      }
    }

    public async Task<List<string>> GetNamespacesAsync()
    {
      if (_k8SClient != null)
      {
        var namespaces = await _k8SClient.CoreV1.ListNamespaceWithHttpMessagesAsync();

        var namespacesList = namespaces.Body.Items.Select(ns => ns.Metadata.Name).ToList();

        _logger.LogInformation("K8SClient.GetNamespacesAsync() returned: {@nsList}", string.Join(", ", [.. namespacesList]));
        return namespacesList;
      }
      else
      {
        return new List<string> { "Empty namespaces list" };
      }
    }

    public async Task<List<string>> GetPodsAsync()
    {
      if (_k8SClient != null)
      {
        var pods = await _k8SClient.CoreV1.ListPodForAllNamespacesWithHttpMessagesAsync();

        var podsList = pods.Body.Items.Select(p => p.Metadata.Name).ToList();

        _logger.LogInformation("K8SClient.GetPodsAsync() returned: {@podsList}", string.Join(", ", [.. podsList]));
        return podsList;
      }
      else
      {
        return new List<string> { "Empty pods list" };
      }
    }

    public async Task<JobCreationResult> CreateJobAsync(string jobName, string namespaceName = "default")
    {
      var jobCreationResult = new JobCreationResult();
      var jobDefinition = CreateJobDefinition(jobName);

      try
      {
        if (_k8SClient != null)
        {
          var httpResponse = await _k8SClient.BatchV1.CreateNamespacedJobWithHttpMessagesAsync(jobDefinition, namespaceName);
          _logger.LogDebug("CreateNamespacedJobWithHttpMessagesAsync() response was: {@response}", httpResponse.Response);

          jobCreationResult.JobName = jobDefinition.Name();
          jobCreationResult.JobNamespaceName = namespaceName;
          jobCreationResult.CreationResult = httpResponse.Response.ReasonPhrase ??= "Error";
          jobCreationResult.JobCreationTime = httpResponse.Response.Headers.Date;
          jobCreationResult.JobContainerImage = jobDefinition.Spec.Template.Spec.Containers[0].Image;
          jobCreationResult.JobNodeSelector = jobDefinition.Spec.Template.Spec.NodeSelector != null ? StringHelper.DictToString(jobDefinition.Spec.Template.Spec.NodeSelector) : "None";

          _logger.LogInformation("Job created: {jobResult}", JsonSerializer.Serialize(jobCreationResult));
        }
      }
      catch (HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
      {
        jobCreationResult.JobName = jobDefinition.Name();
        jobCreationResult.JobNamespaceName = namespaceName;
        jobCreationResult.CreationResult = "Job NOT created: Duplicate";

        _logger.LogError($"Job already exists");
      }
      catch (Exception ex)
      {
        jobCreationResult.JobName = jobDefinition.Name();
        jobCreationResult.JobNamespaceName = namespaceName;
        jobCreationResult.CreationResult = $"Error: see log for Exception {ex.Message}";

        _logger.LogError("Job NOT created: an exception was thrown: {ex}", ex.ToString());
      }
      return jobCreationResult;
    }

    private static V1Job CreateJobDefinition(string jobName)
    {
      var job = new V1Job()
      {
        ApiVersion = "batch/v1",
        Kind = "Job",
        Metadata = new V1ObjectMeta()
        {
            Name = Const.JobsPrefix + jobName
        },
        Spec = new V1JobSpec()
        {
          Template = new V1PodTemplateSpec()
          {
            Spec = new V1PodSpec()
            {
              Containers = new List<V1Container>()
              {
                new()
                {
                  Name = "jet-worker",
                  Image = "acruse2446692s1hubsharedsvc.azurecr.io/bases-jet/jobworker:dev",
                  Env = new List<V1EnvVar>()
                  {
                    new()
                    {
                      Name = "JOB_NAME",
                      Value = jobName
                    },
                    new()
                    {
                      Name = "ITERATIONS",
                      Value = "12"
                    }
                  },
                  ImagePullPolicy = "Always"
                }
              },
              RestartPolicy = "Never",
              //NodeSelector = new Dictionary<string, string>
              //{
              //    { "kubernetes.azure.com/agentpool", "jobs" }
              //}
            }
          }
        }
      };
      return job;
    }
  }
}
