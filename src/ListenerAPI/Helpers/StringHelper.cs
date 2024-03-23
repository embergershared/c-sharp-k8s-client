namespace ListenerAPI.Helpers
{
  public static class StringHelper
  {
    public static string RemoveSbSuffix(string? name)
    {
      return name!.Replace(Constants.Const.SbPublicSuffix, string.Empty);
    }
  }
}
