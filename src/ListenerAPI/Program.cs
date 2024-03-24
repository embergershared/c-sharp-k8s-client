// Using DI with the Azure SDK for .NET to access Azure Service Bus
// Ref: https://learn.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection?tabs=web-app-builder

using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using ListenerAPI.Classes;
using ListenerAPI.Constants;
using ListenerAPI.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ListenerAPI
{
  public class Program
  {
    public static async Task Main(string[] args)
    {
      #region Initialization
      var builder = WebApplication.CreateBuilder(args);
      #endregion

      #region Adding Services
      // Add ASP.NET Controller
      builder.Services.AddControllers();

      // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen();

      // Dependency Injection
      // ###  Kubernetes C# client  ###
      builder.Services.AddSingleton<IK8SClient, K8SClient>();
      //// ###  Dns resolver package  ###
      //builder.Services.AddSingleton<IDnsResolver, DnsResolver>();

      // ###  Azure Clients to use Service Bus(es)  ###
      var sbNamespaces = new List<string>
      {
        builder.Configuration["ServiceBusMainName"] ?? string.Empty,
        builder.Configuration["ServiceBusSecondaryName"] ?? string.Empty
      };

      if (sbNamespaces.Count != 0)
      {
        EnforceTls12();

        builder.Services.AddAzureClients(clientBuilder =>
        {
          clientBuilder.UseCredential(new DefaultAzureCredential());

          // Register ServiceBusClient for each Namespace
          foreach (var sbNamespace in sbNamespaces)
          {
            AddServiceBusClient(clientBuilder, sbNamespace);
          }

          // Set up any default settings
          clientBuilder.ConfigureDefaults(
            builder.Configuration.GetSection("AzureDefaults"));
        });
      }

      // ###  Logging with Seq redirection  ###
        builder.Services.AddLogging(loggingBuilder => {
        loggingBuilder.AddSeq(builder.Configuration.GetSection("Seq"));
      });

      #endregion

      #region Building App
      var app = builder.Build();
      var logger = app.Services.GetRequiredService<ILogger<Program>>();

      // Configure the HTTP request pipeline.
      //if (app.Environment.IsDevelopment())
      //{
      app.UseSwagger();
      app.UseSwaggerUI();
      //}

      //app.UseHttpsRedirection();

      //app.UseAuthorization();

      app.MapControllers();
      #endregion

      logger.LogInformation("ListenerAPI started");
      
      await app.RunAsync();
    }

    private static void AddServiceBusClient(AzureClientFactoryBuilder clientBuilder, string sbName)
    {
      clientBuilder
        .AddServiceBusClientWithNamespace($"{sbName}{Const.SbPublicSuffix}")
        .WithName(sbName)
        .ConfigureOptions(options => options.TransportType = ServiceBusTransportType.AmqpWebSockets);
    }

    private static void EnforceTls12()
    {
      // Enforce TLS 1.2 to connect to Service Bus
      System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
    }
  }
}
