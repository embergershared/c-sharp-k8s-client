namespace ListenerAPI.Models
{
  public class JobRequest
  {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public string JobName { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
  
    public int MessagesToCreateCount { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public SbNsQueue SbNsQueue { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public int? JobId { get; set; }
    public string? Parameter1 { get; set; }
    public string? Parameter2 { get; set; }

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
