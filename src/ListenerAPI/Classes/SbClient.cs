using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using ListenerAPI.Constants;
using ListenerAPI.Interfaces;
using Microsoft.Extensions.Logging;

namespace ListenerAPI.Classes
{
  public class SbClient : ISbClient
  {
    private readonly ILogger<SbClient> _logger;
    private ServiceBusClient? _sbClient;
    private readonly ServiceBusClientOptions? _clientOptions;

    public SbClient(
      ILogger<SbClient> logger
    )
    {
      _logger = logger;
      _logger.LogInformation("SbClient constructed");
      _clientOptions = GetServiceBusClientOptions();
    }

    public ServiceBusClient? CreateClientMI(string sbNamespace, string? clientId = null)
    {
      _logger.LogInformation("SbClient.CreateClientMIAsync() called");

      // Create an Azure ServiceBusClient that will authenticate through Active Directory

      // Reference for the AzSbClient: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.servicebus-readme?view=azure-dotnet
      // Reference for authentication with Azure.Identity: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.servicebus-readme?view=azure-dotnet#authenticating-with-azureidentity

      // See Client lifetime recommendations for wider use out of this POC: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.servicebus-readme?view=azure-dotnet#client-lifetime

      _logger.LogDebug("Creating a SbClient to the namespace: {@sb_ns}, with MI \"{@client_id}\"", sbNamespace, clientId);
      var fullyQualifiedNamespace = $"{sbNamespace}.{Const.SbPublicSuffix}";

      try
      {
        if (string.IsNullOrEmpty(clientId))
        {
          // Code for system-assigned managed identity:
          _sbClient = new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());
        }
        else
        {
          // Code for user-assigned managed identity:
          var credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
              ManagedIdentityClientId = clientId
            });
          _sbClient = new ServiceBusClient(fullyQualifiedNamespace, credential, _clientOptions);
        }

        _logger.LogInformation($"ServiceBusClient created");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"ServiceBusClient creation failed");
      }

      return _sbClient;
    }

    public ServiceBusClient? CreateClientCS(string connString)
    {
      _logger.LogInformation("SbClient.CreateClientCS() called");

      // Create an Azure ServiceBusClient that will use a connection string
      // Ref: https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?tabs=connection-string

      // Use the connection string to create a client.
      try
      {
        _sbClient = new ServiceBusClient(connString, _clientOptions);

        _logger.LogInformation($"ServiceBusClient created");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"ServiceBusClient creation failed");
      }

      return _sbClient;
    }

    private static ServiceBusClientOptions GetServiceBusClientOptions()
    {
      // Enforce TLS 1.2 to connect to Service Bus
      System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

      var clientOptions = new ServiceBusClientOptions()
      {
        TransportType = ServiceBusTransportType.AmqpWebSockets
      };
      return clientOptions;
    }

    public async ValueTask DisposeAsync()
    {
      _logger.LogDebug("SbClient.DisposeAsync() called");

      if (_sbClient != null)
      {
        await _sbClient.DisposeAsync();
      }
      GC.SuppressFinalize(this);
    }
  }
}
