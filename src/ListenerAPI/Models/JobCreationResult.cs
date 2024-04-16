using System;

namespace ListenerAPI.Models
{
  public class JobCreationResult
  {
    public string? CreationResult { get; set; }
    public DateTimeOffset? JobCreationTime { get; set; }
    public string? JobName { get; set; }
    public string? JobNamespaceName { get; set; }
    public string? JobContainerImage { get; set; }
    public string? JobNodeSelector { get; set; }
  }
}
