using System.Globalization;

namespace ListenerAPI.Constants
{
  public class ConfigKey
  {
    // appsettings.json / Application Settings / Environment keys

    // Azure Identity preference for local environment
    internal const string AzureIdentityPreferredAuthProfile = "PreferredAzureAuth";

    // Service Bus keys
    internal const string SbNsQueueName = "AzSbNsQueueName";
    internal const string SbProcessorIsUsed = "StartMessagesProcessor";

    // Kubernetes Jobs keys
    internal const string JobsNamespace = "JobsNamespace";
    internal const string JobsPrefix = "JobsPrefix";
    internal const string JobsRepository = "JobsRepository";
    internal const string JobsImageName = "JobsImageName";
    internal const string JobsImageTag = "JobsImageTag";
    internal const string JobsCpuRequest = "JobsCpuRequest";
    internal const string JobsMemoryRequest = "JobsMemoryRequest";
    internal const string JobsNodeSelKey = "JobsNodeSelectorKey";     // For Agent pool: "kubernetes.azure.com/agentpool"
    internal const string JobsNodeSelValue = "JobsNodeSelectorValue"; // For Agent pool: "jobs"
    internal const string JobsTtlAfterFinished = "JobsTtlAfterFinished";
    internal const string JobsActiveDeadline = "JobsActiveDeadline";
  }
}
