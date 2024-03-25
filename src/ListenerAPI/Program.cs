// Using DI with the Azure SDK for .NET to access Azure Service Bus Client
// Ref: https://learn.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection?tabs=web-app-builder
// Using Azure Service Bus Quickstart
// Ref: https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?tabs=passwordless

using Azure.Messaging.ServiceBus;
using ListenerAPI.Classes;
using ListenerAPI.Constants;
using ListenerAPI.Helpers;
using ListenerAPI.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Threading.Tasks;

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

      // Add Swagger/OpenAPI
      // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
      //builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen(options =>
      {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
          Version = "v1",
          Title = "JET Listener & Jobs",
          Description = "A Listener that generates Kubernetes Jobs when receiving Service Bus messages.",
          TermsOfService = new Uri("https://example.com/terms"),
          Contact = new OpenApiContact
          {
            Name = "Contact",
            Url = new Uri("https://example.com/contact")
          },
          License = new OpenApiLicense
          {
            Name = "License",
            Url = new Uri("https://example.com/license")
          }
        });
      });

      // Add QueuesProcessor background service
      builder.Services.AddHostedService<QueuesProcessor>();

      // ======  Dependency Injection  ======
      // ###  Kubernetes C# client  ###
      builder.Services.AddSingleton<IK8SClient, K8SClient>();

      // ###  Azure Clients to use Service Bus(es)  ###
      var sbNamespaces = AppGlobal.GetServiceBusNames(builder.Configuration);
      AppGlobal.Data["IsUsingServiceBus"] = (sbNamespaces.Count != 0).ToString();
      EnforceTls12();
      builder.Services.AddAzureClients(clientBuilder =>
      {
        clientBuilder.UseCredential(AzureCreds.GetCred(builder.Configuration["PreferredAzureAuth"]));

        // Create a dumb default client to avoid queues controller crash at creation (so we can send a 404)
        clientBuilder.AddServiceBusClientWithNamespace($"dumb{Const.SbPublicSuffix}");

        // Register ServiceBusClient(s) for each Namespace(s)
        foreach (var sbNamespace in sbNamespaces)
        {
          AddServiceBusClient(clientBuilder, sbNamespace!);
        }

        // Set up any default settings
        clientBuilder.ConfigureDefaults(
          builder.Configuration.GetSection("AzureDefaults"));
      });

      // ###  ServiceBus Queues  ###
      builder.Services.AddTransient<IServiceBusQueues, ServiceBusQueues>();

      // ======  Logging  ======
      // ###  Logging with Seq redirection  ###
      builder.Services.AddLogging(loggingBuilder => {
        loggingBuilder.AddSeq(builder.Configuration.GetSection("Seq"));
      });

      #endregion

      #region Building AppGlobal
      var app = builder.Build();
      var logger = app.Services.GetRequiredService<ILogger<Program>>();

      // Configure the HTTP request pipeline.
      //if (app.Environment.IsDevelopment())
      //{
      logger.LogInformation("Adding Swagger + Swagger UI to the app");
      app.UseSwagger();
      app.UseSwaggerUI(options =>
      {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
      });

      logger.LogInformation("Adding DeveloperExceptionPage to the app");
      app.UseDeveloperExceptionPage();
      //}

      //app.UseHttpsRedirection();

      //app.UseAuthorization();

      logger.LogInformation("Adding Controllers to the app");
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
