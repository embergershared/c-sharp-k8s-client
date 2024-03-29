using Microsoft.Extensions.Configuration;

namespace ListenerAPI.Models
{
  public class SbNsQueue
  {
    private const string SplitCharacter = "/";

    public string? SbNamespace { get; set; }
    public string? QueueName { get; set; }

    public SbNsQueue(IConfiguration config, string configKey)
    {
      SplitArgument(config[configKey]!);
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
    public SbNsQueue()
    {
    }

    public override string ToString()
    {
      return $"{SbNamespace}{SplitCharacter}{QueueName}";
    }
    public bool IsValid()
    {
      return !string.IsNullOrEmpty(SbNamespace) && !string.IsNullOrEmpty(QueueName);
    }

    private void SplitArgument(string argument)
    {
      this.SbNamespace = (argument.Split(SplitCharacter))[0];
      this.QueueName = (argument.Split(SplitCharacter))[1];
    }
  }
}
