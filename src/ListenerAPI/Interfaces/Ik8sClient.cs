using ListenerAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListenerAPI.Interfaces
{
    public interface IK8SClient
    {
        //List<string> GetNamespaces();
        Task<List<string>> GetNamespacesAsync();

        Task<List<string>> GetPodsAsync();

        Task<JobCreationResult> CreateJobAsync(string jobName, string? namespaceName);
    }
}
