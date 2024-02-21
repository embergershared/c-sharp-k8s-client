﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Autorest;
using k8s.Models;
using ListenerAPI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ListenerAPI.Classes
{
    using CreateLambda = Func<IKubernetes, IKubernetesObject, string, CancellationToken, Task<IKubernetesObject>>;

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
            _logger.LogInformation($"Found in config: \"kubeconfigPath\": \"{kPath}\"");

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
                _logger.LogError(ex.ToString());
            }
        }

        public async Task<List<string>> GetNamespacesAsync()
        {
            if (_k8SClient != null)
            {
                var namespaces = await _k8SClient.CoreV1.ListNamespaceWithHttpMessagesAsync();

                var namespacesList = namespaces.Body.Items.Select(ns => ns.Metadata.Name).ToList();

                _logger.LogInformation($"Returned: " +
                                       string.Join(", ", namespacesList.ToArray())); 
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

                _logger.LogInformation($"Returned: " +
                                       string.Join(", ", podsList.ToArray()));
                return podsList;
            }
            else
            {
                return new List<string> { "Empty pods list" };
            }
        }

        public async Task CreateJob(string jobName, string namespaceName = "bases-jet")
        {
            var job = new V1Job()
            {
                ApiVersion = "batch/v1",
                Kind = "Job",
                Metadata = new V1ObjectMeta()
                {
                    Name = jobName
                },
                Spec = new V1JobSpec()
                {
                    Template = new V1PodTemplateSpec()
                    {
                        Spec = new V1PodSpec()
                        {
                            Containers = new List<V1Container>()
                            {
                                new V1Container()
                                {
                                    Name = "pi",
                                    Image = "perl",
                                    Command = new List<string>()
                                    {
                                        "perl"
                                    },
                                    Args = new List<string>()
                                    {
                                        "-Mbignum=bpi",
                                        "-wle",
                                        "print bpi(2000)"
                                    },
                                    Env = new List<V1EnvVar>()
                                    {
                                        new()
                                        {
                                            Name = "JOB_NAME",
                                            Value = jobName
                                        },
                                        new()
                                        {
                                            Name = "INPUT",
                                            Value = Guid.NewGuid().ToString()
                                        }
                                    }
                                }
                            },
                            RestartPolicy = "Never"
                        }
                    }
                }
            };

            try
            {
                if (_k8SClient != null)
                {
                    await _k8SClient.BatchV1.CreateNamespacedJobWithHttpMessagesAsync(job, "bases-jet");
                }
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogError($"Job already exists");
            }
            catch (Exception e)
            {
                _logger.LogError($"An error occurred while trying to create resource: {e}");
            }
        }
    }
}