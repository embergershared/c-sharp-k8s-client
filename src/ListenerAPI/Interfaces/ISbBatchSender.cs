using System.Threading.Tasks;

namespace ListenerAPI.Interfaces
{
    public interface ISbBatchSender
    {
      Task SendMessagesAsync(int numOfMessages);
    }
}