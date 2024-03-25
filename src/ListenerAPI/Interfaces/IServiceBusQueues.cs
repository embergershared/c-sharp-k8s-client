using ListenerAPI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListenerAPI.Interfaces
{
  public interface IServiceBusQueues : IAsyncDisposable
  {

    Task AddReceiveMessageBatchFromQueuesTasksAsync(string sbName, List<Task<IReadOnlyList<ReceivedMessage>>> tasks, int batchSize);

    Task AddSenderToQueuesTasksAsync(int value, string sbName, List<Task<int>> tasks);

    Task AddDeleteAllFromQueuesTasksAsync(string sbName, List<Task<int>> tasks);

    new ValueTask DisposeAsync();
  }
}
