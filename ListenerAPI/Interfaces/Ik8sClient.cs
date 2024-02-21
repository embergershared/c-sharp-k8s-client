using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListenerAPI.Interfaces
{
    public interface IK8SClient
    {
        //List<string> GetNamespaces();
        Task<List<string>> GetNamespacesAsync();

        Task<List<string>> GetPodsAsync();

        Task CreateJob(string jobName, string namespaceName);
    }
}
