using System;
using System.Collections.Generic;

namespace ListenerAPI.Classes
{
  internal static class AppGlobal
  {
    internal static Dictionary<string, string> Data = new()
    {
      {"InitializedOn", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}
    };
  }
}
