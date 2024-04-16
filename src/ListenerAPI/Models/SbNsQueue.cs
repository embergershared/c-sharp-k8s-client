using Microsoft.Extensions.Configuration;
using System.ComponentModel;

namespace ListenerAPI.Models
{
  public class SbNsQueue
  {
    private const string SplitCharacter = "/";

    // Public Properties
    [DefaultValue("sb-use2-446692-s4-aksafdpls-01")]
    public string? SbNamespace { get; set; }

    [DefaultValue("jobrequests")]
    public string? QueueName { get; set; }

    // Constructors
    public SbNsQueue()
    {
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
