using System.ComponentModel;
#pragma warning disable CS8618

namespace ListenerAPI.Models
{
  public class JobRequestMessageBody
  {
    // Public Properties
    public string JobName { get; set; }
    
    public int? JobId { get; set; }
    
    public string? Parameter1 { get; set; }
    
    public string? Parameter2 { get; set; }
  }
}
