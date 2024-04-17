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

    #region Interface implementation
    public async Task<List<string>> GetNamespacesAsync()
    {
      if (_k8SClient != null)
      {
        var namespaces = await _k8SClient.CoreV1.ListNamespaceWithHttpMessagesAsync();

        var namespacesList = namespaces.Body.Items.Select(ns => ns.Metadata.Name).ToList();

        _logger.LogInformation("K8SClient.GetNamespacesAsync() returned: {@nsList}", StringHelper.ListToSeparatedString(namespacesList));
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

        _logger.LogInformation("K8SClient.GetPodsAsync() returned: {@podsList}", StringHelper.ListToSeparatedString(podsList));
        return podsList;
      }
      else
      {
        return new List<string> { "Empty pods list" };
      }
    }

    public async Task<JobCreationResult> CreateJobAsync(string jobName, string? namespaceName = "default")
    {
      var jobCreationResult = new JobCreationResult();
      var jobDefinition = CreateJobDefinition(jobName);

      try
      {
        if (_k8SClient != null)
        {
          var httpResponse = await _k8SClient.BatchV1.CreateNamespacedJobWithHttpMessagesAsync(jobDefinition, namespaceName);
          _logger.LogDebug("CreateNamespacedJobWithHttpMessagesAsync() response was: {@response}", httpResponse.Response);

          if (httpResponse.Response.IsSuccessStatusCode)
          {
            jobCreationResult.IsSuccess = true;
            jobCreationResult.ResultMessage = 
              $"Successfully created job.batch/{jobDefinition.Metadata.Name}," + 
              $" in namespace: {namespaceName}," +
              $" at: {httpResponse.Response.Headers.Date ?? DateTime.Now}," + 
              $" with image: {jobDefinition.Spec.Template.Spec.Containers[0].Image}," +
              $" and nodeSelector: {(jobDefinition.Spec.Template.Spec.NodeSelector != null ? StringHelper.DictToString(jobDefinition.Spec.Template.Spec.NodeSelector) : "none")}" +
              ".";

            _logger.LogInformation("Job created: {jobResult}", JsonSerializer.Serialize(jobCreationResult));
          }
          else
          {
            var message = $"Job {jobDefinition.Metadata.Name} NOT created: an error happened: {httpResponse.Response.ReasonPhrase}";
            jobCreationResult.ResultMessage = message;

            _logger.LogError("Job NOT created: an error happened: {message}", message);
          }
        }
      }
      catch (HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
      {
        jobCreationResult.ResultMessage = $"Job {namespaceName}/{jobDefinition.Metadata.Name} already exists => NOT created.";

        _logger.LogWarning("Job NOT created: Duplicate => Job {namespace}/{job} already exists", namespaceName, jobDefinition.Metadata.Name);
      }
      catch (Exception ex)
      {
        jobCreationResult.ResultMessage = $"Exception occurred: {ex.Message}";

        _logger.LogError("Job NOT created: an exception was thrown: {ex}", ex.ToString());
      }
      return jobCreationResult;
    }
    #endregion

    #region Private methods
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

    private V1Job CreateJobDefinition(string jobName)
    {
      var job = new V1Job()
      {
        ApiVersion = "batch/v1",
        Kind = "Job",
        Metadata = new V1ObjectMeta()
        {
            Name = $"{_config.GetValue<string>(ConfigKey.JobsPrefix)}-{jobName}"
        },
        Spec = new V1JobSpec()
        {
          BackoffLimit = 1,
          TtlSecondsAfterFinished = _config.GetValue<int>(ConfigKey.JobsTtlAfterFinished),
          ActiveDeadlineSeconds = _config.GetValue<int>(ConfigKey.JobsActiveDeadline),
          Template = new V1PodTemplateSpec()
          {
            Spec = new V1PodSpec()
            {
              Containers = new List<V1Container>()
              {
                new()
                {
                  Name = Const.JobsContainerName,
                  Image = $"{_config.GetValue<string>(ConfigKey.JobsRepository)}/{_config.GetValue<string>(ConfigKey.JobsImageName)}:{_config.GetValue<string>(ConfigKey.JobsImageTag)}",
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
                  Resources = new V1ResourceRequirements()
                  {
                    Requests = new Dictionary<string, ResourceQuantity>
                    {
                      { "cpu", new ResourceQuantity(_config.GetValue<string>(ConfigKey.JobsCpuRequest)) },
                      { "memory", new ResourceQuantity(_config.GetValue<string>(ConfigKey.JobsMemoryRequest)) }
                    },
                  },  
                  ImagePullPolicy = "Always"
                }
              },
              RestartPolicy = "Never",
              NodeSelector = new Dictionary<string, string>
              {
                { _config.GetValue<string>(ConfigKey.JobsNodeSelKey)   ?? "kubernetes.azure.com/mode",
                  _config.GetValue<string>(ConfigKey.JobsNodeSelValue) ?? "user" }
              }
            }
          }
        }
      };
      return job;
    }
    #endregion
  }
}
