using Microsoft.Extensions.Configuration;
using System.ComponentModel;

namespace ListenerAPI.Models
{
  public class SbNsQueue
  {
    private const string SplitCharacter = "/";

    // Public Properties
    [Description("Optional Service Bus Namespace name")]
    [DefaultValue("Optional")]
    public string? SbNamespace { get; set; }

    [Description("Optional Service Bus Queue name")]
    [DefaultValue("Optional")]
    public string? QueueName { get; set; }

    // Constructors
    public SbNsQueue()
    {
      //SbNamespace= AppGlobal.Data["DefaultSbNamespace"];
      //QueueName = AppGlobal.Data["DefaultQueueName"];
    }
    public SbNsQueue(IConfiguration config, string configKey)
    {
      var configValue = config[configKey];
      if (configValue != null) SplitArgument(configValue);
    }
    public SbNsQueue(string nsQueueName)
    {
      SplitArgument(nsQueueName);
    }
    public SbNsQueue(string sbNamespace, string queueName)
    {
      this.SbNamespace = sbNamespace;
      this.QueueName = queueName;
    }

    // Public Methods
    public override string ToString()
    {
      return $"{SbNamespace}{SplitCharacter}{QueueName}";
    }
    public bool IsValid()
    {
      return !string.IsNullOrEmpty(SbNamespace) && !string.IsNullOrEmpty(QueueName);
    }

    // Private Methods
    private void SplitArgument(string argument)
    {
      this.SbNamespace = (argument.Split(SplitCharacter))[0];
      this.QueueName = (argument.Split(SplitCharacter))[1];
    }
  }
}
