using System;
using System.Threading.Tasks;
using ListenerAPI.Classes;
using ListenerAPI.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

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
      builder.Services.AddSingleton<IK8SClient, K8SClient>();
      builder.Services.AddSingleton<IDnsResolver, DnsResolver>();

      builder.Services.AddSingleton<ISbClient, SbClient>(); // 1 per namespace to create 1 AMQP connection. Should be tied to application lifecycle, as per: https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.servicebus.servicebusclient?view=azure-dotnet#remarks
      builder.Services.AddSingleton<Func<ISbClient>>(x => () => x.GetService<ISbClient>()!); // Factory to generate ISbClient singleton instances per ServiceBus namespaces, if needed
      builder.Services.AddTransient<ISbSender, SbSender>(); // Connects only to send batch messages, then disconnects
      #endregion

      #region Building App
      var app = builder.Build();

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

      await app.RunAsync();
    }
  }
}
