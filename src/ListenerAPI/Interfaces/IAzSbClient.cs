//using System.Threading.Tasks;
//using Azure.Messaging.ServiceBus;

//namespace ListenerAPI.Interfaces
//{
//  internal interface IAzSbClient
//  {
//    // Service Bus client operations
//    bool CreateClient(string sbNamespace, string? clientId = null);
//    Task DisposeClientAsync();

//    // Message operations
//    Task<bool> SendMessageAsync(string queue, string message);
//    Task<ServiceBusReceivedMessage?> ReceiveMessageAsync(string queue);

//    Task<bool> DeleteAllMessagesAsync(string queue);
//  }
//}
