using ListenerAPI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListenerAPI.Interfaces
{
  public interface ISbMessages : IAsyncDisposable
  {
    Task AddSendMessagesToQueuesTasksAsync(int value, string sbName, List<Task<int>> tasks);

    Task AddReceiveMessagesBatchesFromQueuesTasksAsync(string sbName, List<Task<IReadOnlyList<ReceivedMessage>>> tasks, int batchSize);

    Task AddDeleteAllMessagesFromQueuesTasksAsync(string sbName, List<Task<int>> tasks);

    new ValueTask DisposeAsync();
  }
}
