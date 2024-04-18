using System.ComponentModel;
#pragma warning disable CS8618

namespace ListenerAPI.Models
{
  public class JobRequest
  {
    // Public Properties
    [DefaultValue("DefaultJobRequestJobName")]
    public string JobName { get; set; }
    
    [DefaultValue(1)]
    public int MessagesToCreateCount { get; set; }

    [Description("Optional Service Bus Namespace/Queue names")]
    public SbNsQueue? SbNsQueue { get; set; }

    [Description("Optional JobId")]
    public int? JobId { get; set; }

    [Description("Optional Parameter1")]
    [DefaultValue("Parameter1DefaultValue")]
    public string? Parameter1 { get; set; }

    [Description("Optional Parameter2")]
    [DefaultValue("Parameter2DefaultValue")]
    public string? Parameter2 { get; set; }

    // Public Methods
    public bool IsValid(out string? error)
    {
      if (string.IsNullOrEmpty(JobName))
      {
        error = "JobName is required";
        return false;
      }

      if (MessagesToCreateCount < 1)
      {
        error = "messagesToCreateCount must be greater than 0";
        return false;
      }

      error = null;
      return true;
    }
  }
}
