using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WorkerJob
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            #region Initialization
            var builder = Host.CreateApplicationBuilder(args);
            #endregion

            #region Adding Services
            builder.Services.AddHostedService<Worker>();
            builder.Services.AddTransient<IJob, Job>();
            builder.Services.AddLogging(loggingBuilder => { 
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSimpleConsole(consoleFormatterOptions =>
                {
                    consoleFormatterOptions.IncludeScopes = true;
                    consoleFormatterOptions.SingleLine = true;
                    consoleFormatterOptions.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                });
            });
            #endregion

            #region Building App
            var host = builder.Build();
            #endregion

            await host.RunAsync();
        }
    }
}