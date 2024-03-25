namespace ListenerAPI.Constants
{
  internal static class Const
  {
    internal const string JobsPrefix = "jet-jobs-";
    internal const string K8SNsName = "bases-jet";
    internal const string SbPublicSuffix = ".servicebus.windows.net";
    internal const string SbPrivateSuffix = ".privatelink.servicebus.windows.net";
    internal static readonly string[] SbNamesConfigKeyNames = ["ServiceBus01Name"];
    internal static readonly string SbProcessorQueueConfigKeyName = "ProcessorQueueName";
  }
}
