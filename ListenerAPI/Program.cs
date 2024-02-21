using System.Threading.Tasks;
using ListenerAPI.Classes;
using ListenerAPI.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
