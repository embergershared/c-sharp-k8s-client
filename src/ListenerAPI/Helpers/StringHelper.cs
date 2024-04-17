using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;

namespace ListenerAPI.Helpers
{
  public static class StringHelper
  {
    private const string KvSeparator = ";";

    public static string RemoveSbSuffix(string? name)
    {
      return name!.Replace(Constants.Const.SbPublicSuffix, string.Empty);
    }

    public static string DictToString(IDictionary<string, string> dict)
    {
      var sb = new StringBuilder();
      foreach (var kvp in dict)
      {
        sb.Append($"{kvp.Key}={kvp.Value}{KvSeparator}");
      }
      return sb.Remove(sb.Length - 1, 1).ToString();
    }

    public static string ListToSeparatedString(IList<string> list, string separator = ", ")
    {
      var value = list.Aggregate(string.Empty, (current, st) => current + (st + separator));
      return value.TrimEnd(separator.ToCharArray());
    }
  }
}
