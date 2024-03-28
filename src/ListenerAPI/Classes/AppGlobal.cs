using ListenerAPI.Constants;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using ListenerAPI.Models;

namespace ListenerAPI.Classes
{
  internal static class AppGlobal
  {
    internal static Dictionary<string, string> Data = new()
    {
      {"InitializedOn", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}
    };

    internal static List<string?> GetServiceBusNames(IConfiguration config)
    {
      var sbNamespaces = Const.SbNamesConfigKeyNames
        .Select(key => config[key])
        .Where(sb => !string.IsNullOrEmpty(sb)).ToList();

      sbNamespaces.Add((GetServiceBusName(config, Const.SbProcessorQueueConfigKeyName)));
      sbNamespaces.Add((GetServiceBusName(config, Const.SbMessagesTargetConfigKeyName)));

      return sbNamespaces.Distinct().ToList();
    }

    internal static string? GetServiceBusName(IConfiguration config, string configKey)
    {
      var sbQueue = new SbNsQueue(config, configKey);
      return sbQueue.SbNamespace;
    }
  }
}
