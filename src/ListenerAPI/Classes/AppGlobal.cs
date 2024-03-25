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
      return sbNamespaces;
    }

    internal static SbNsQueue GetNames(IConfiguration config)
    {
      var appSettingValue = config[Const.SbProcessorQueueConfigKeyName]!.Split("/");
      var sbNsQueue = new SbNsQueue
      {
        SbNamespace = appSettingValue[0],
        QueueName = appSettingValue[1],
      };
      return sbNsQueue;
    }
  }
}
