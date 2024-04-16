using System.Collections.Generic;
using System.Text;

namespace ListenerAPI.Helpers
{
  public static class StringHelper
  {
    public static string RemoveSbSuffix(string? name)
    {
      return name!.Replace(Constants.Const.SbPublicSuffix, string.Empty);
    }

    public static string DictToString(IDictionary<string, string> dict)
    {
      var sb = new StringBuilder();
      foreach (var kvp in dict)
      {
        sb.Append($"{kvp.Key}={kvp.Value};");
      }
      return sb.ToString();
    }
  }
}
