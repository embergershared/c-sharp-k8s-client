using Azure.Messaging.ServiceBus;
using System;
using System.Threading.Tasks;

namespace ListenerAPI.Interfaces
{
  public interface ISbClient: IAsyncDisposable
  {
    ServiceBusClient? CreateClientMI(string sbNamespace, string? clientId = null);
    ServiceBusClient? CreateClientCS(string connString);
    
    new ValueTask DisposeAsync();
  }
}
