using ListenerAPI.Constants;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using ListenerAPI.Models;
using System.Text.Json;

namespace ListenerAPI.Helpers
{
    internal static class AppGlobal
    {
        internal static Dictionary<string, string> Data = new()
    {
      {"InitializedOn", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}
    };

        internal static JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        internal static List<string?> GetServiceBusNames(IConfiguration config)
        {
            var sbNamespaces = new List<string?>
            {
              //Const.SbNamesConfigKeyNames
              //  .Where(sb => !string.IsNullOrEmpty(sb)).ToList();
              //  .Select(key => config[key])
              //GetServiceBusName(config, Const.SbProcessorQueueConfigKeyName),
              GetServiceBusName(config, ConfigKey.SbNsQueueName)
            };

            return sbNamespaces.Distinct().ToList();
        }

        internal static string? GetServiceBusName(IConfiguration config, string configKey)
        {
            var sbQueue = new SbNsQueue(config, configKey);
            return sbQueue.SbNamespace;
        }
    }
}
