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

      sbNamespaces.Add((GetNames(config, Const.SbProcessorQueueConfigKeyName)).SbNamespace);
      sbNamespaces.Add((GetNames(config, Const.SbMessagesTargetConfigKeyName)).SbNamespace);

      return sbNamespaces.Distinct().ToList();
    }

    internal static SbNsQueue GetNames(IConfiguration config, string configKey)
    {
      var appSettingValue = config[configKey]!.Split("/");
      var sbNsQueue = new SbNsQueue
      {
        SbNamespace = appSettingValue[0],
        QueueName = appSettingValue[1],
      };
      return sbNsQueue;
    }

    internal static SbNsQueue GetNames(string nsQueueName)
    {
      var appSettingValue = nsQueueName!.Split("/");
      var sbNsQueue = new SbNsQueue
      {
        SbNamespace = appSettingValue[0],
        QueueName = appSettingValue[1],
      };
      return sbNsQueue;
    }
  }
}
