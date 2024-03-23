using System;
using System.Threading.Tasks;
using ListenerAPI.Classes;
using ListenerAPI.Factories;
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

      builder.Services.AddSingletonFactory<ISbClient, SbClient>(); // Factory to generate ISbClient singleton instances per ServiceBus namespaces, if needed
      builder.Services.AddTransientFactory<ISbSender, SbSender>(); // Factory to generate ISbSender transient instances per senders on a namespace queue, that connects only to send batch messages, then disconnects

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
