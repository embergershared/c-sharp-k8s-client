using System.Text.Json.Serialization;

namespace ListenerAPI.Models
{
  public class ReceivedMessage
  {
    [JsonIgnore]
    public bool IsSuccessfullyReceived { get; set; } = false;
    public long? SeqNumber { get; set; }
    public string? Body { get; set; }
    public string? MessageId { get; set; }
    public string? ServiceBusName { get; set; }
    public string? QueueName { get; set; }
  }
}
