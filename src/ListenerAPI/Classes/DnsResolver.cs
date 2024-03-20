using DnsClient;
using ListenerAPI.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ListenerAPI.Classes
{
    public class DnsResolver : IDnsResolver
    {
        // Private members
        private readonly ILogger<DnsResolver> _logger;

        // Constructor
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "Classes can't use primary constructors")]
        public DnsResolver(ILogger<DnsResolver> logger)
        {
            _logger = logger;
        }

        // Interface implementation
        public async Task<bool> CanResolveAsync(string hostname)
        {
            _logger.LogInformation("Resolving: {@hostname}", hostname);
            var lookup = new LookupClient();
            var result = await lookup.QueryAsync(hostname, QueryType.A);

            if (result.HasError)
            {
                _logger.LogError("Impossible to resolve {@hostname}", hostname);
                return false;
            }
            else
            {
                _logger.LogInformation("Results from DNS Server: {@ns_name}", result.NameServer.ToString());
                foreach (var nsRecord in result.Answers)
                {
                    _logger.LogInformation("Record: {@ns_record}", nsRecord.ToString());
                }

                return true;
            }
        }
    }
}
