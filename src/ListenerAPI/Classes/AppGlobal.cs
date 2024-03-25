using ListenerAPI.Constants;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

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
  }
}
