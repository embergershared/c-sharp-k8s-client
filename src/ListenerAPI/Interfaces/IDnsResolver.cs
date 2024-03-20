using System.Threading.Tasks;

namespace ListenerAPI.Interfaces
{
    internal interface IDnsResolver
    {
        Task<bool> CanResolveAsync(string hostname);
    }
}
