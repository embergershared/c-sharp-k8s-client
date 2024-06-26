﻿using ListenerAPI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ListenerAPI.Interfaces
{
  public interface ISbMessages : IAsyncDisposable
  {
    // Per Service Bus namespace operations
    Task AddSendMessagesTo1NsAllQueuesTasksAsync(JobRequest jobRequest, string sbName, List<Task<int>> tasks);
    Task AddReceiveMessagesBatchesFrom1NsAllQueuesTasksAsync(string sbName, List<Task<IReadOnlyList<ReceivedMessage>>> tasks, int batchSize);
    Task AddDeleteAllMessagesFrom1NsAllQueuesTasksAsync(string sbName, List<Task<int>> tasks);

    // Per Service Bus namespace + queue operations
    bool AddSendMessagesTo1Ns1QueueTask(JobRequest jobRequest, List<Task<int>> sendTasksList);
    bool AddDeleteAllMessagesFrom1Ns1QueueTask(string sbName, string qName, List<Task<int>> tasks);

    // Clean Dispose()
    new ValueTask DisposeAsync();
  }
}
