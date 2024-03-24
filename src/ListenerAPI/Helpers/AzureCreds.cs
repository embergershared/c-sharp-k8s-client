using Azure.Core;
using Azure.Identity;

namespace ListenerAPI.Helpers
{
  public static class AzureCreds
  {
    public static ChainedTokenCredential GetCred(string? pref)
    {
      return pref switch
      {
        "VS" => new ChainedTokenCredential(new VisualStudioCredential(), new DefaultAzureCredential()),
        "CLI" => new ChainedTokenCredential(new AzureCliCredential(), new DefaultAzureCredential()),
        _ => new ChainedTokenCredential(new DefaultAzureCredential()),
      };

      // Credential Classes reference:
      // https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme?view=azure-dotnet#credential-classes
    }
  }
}
