using System.Threading.Tasks;

namespace WorkerJob
{
    public interface IJob
    {
        Task ExecuteAsync();
    }
}