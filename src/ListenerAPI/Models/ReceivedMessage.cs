using System;
using System.Text.Json.Serialization;

namespace ListenerAPI.Models
{
  public class ReceivedMessage
  {
    // Additions
    [JsonIgnore]
    public bool IsSuccessfullyReceived { get; set; } = false;
    public string? ServiceBusName { get; set; }
    public string? QueueName { get; set; }
    public string? BodyString => Body?.ToString();

    // From ServiceBusReceivedMessage Class
    // Ref: https://docs.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusreceivedmessage?view=azure-dotnet
    public long? SequenceNumber { get; set; }
    public BinaryData? Body { get; set; }
    public string? MessageId { get; set; }
    public string? PartitionKey { get; set; }
    public TimeSpan? TimeToLive { get; set; }
    public string? ContentType { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset? EnqueuedTime { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
  }
}
