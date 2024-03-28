using Microsoft.Extensions.Configuration;

namespace ListenerAPI.Models
{
  public class SbNsQueue
  {
    private const string SplitCharacter = "/";

    public string? SbNamespace;
    public string? QueueName;

    public SbNsQueue(IConfiguration config, string configKey)
    {
      SplitArgument(config[configKey]!);
    }
    public SbNsQueue(string nsQueueName)
    {
      SplitArgument(nsQueueName);
    }

    public override string ToString()
    {
      return $"{SbNamespace}{SplitCharacter}{QueueName}";
    }

    private void SplitArgument(string argument)
    {
      this.SbNamespace = (argument.Split(SplitCharacter))[0];
      this.QueueName = (argument.Split(SplitCharacter))[1];
    }
  }
}
