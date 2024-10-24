using System.Collections.Concurrent;
using Microsoft.VisualStudio.Threading;
using snhw.Common;

namespace snhw.Workers
{
    internal sealed class RequestManager : BackgroundService
    {
        private CancellationToken cancellationToken;

        private readonly SemaphoreSlim taskQueueSemaphore = Queues.RequestTaskQueueSemaphore;
        private readonly ConcurrentQueue<Task<IResult>> taskQueue = Queues.RequestTaskQueue;

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Task<IResult> processedTask;

            this.cancellationToken = cancellationToken;
            AsyncManualResetEvent executorFinishSignal = new AsyncManualResetEvent();
            var tasksWaitingList = new List<Task<IResult>>();

            while (!cancellationToken.IsCancellationRequested || !taskQueue.IsEmpty)
            {
                try
                {
                    await taskQueueSemaphore.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    if (taskQueue.IsEmpty)
                        break;
                }

                if (!taskQueue.TryDequeue(out processedTask))
                {
                    continue;
                }

                tasksWaitingList.Add(processedTask);
                
                while (tasksWaitingList.Count > 0)
                {
                    var isFirstTask = true;
                    foreach (var taskInWaitingList in tasksWaitingList.ToList())
                    {
                        if (isFirstTask)
                        {
                            isFirstTask = false;
                            executorFinishSignal.Reset();
                        }

                        _ = Task.Run(async () => await taskInWaitingList);
                        tasksWaitingList.Remove(taskInWaitingList);
                    }

                    if (taskQueueSemaphore.CurrentCount > 0 || tasksWaitingList.Count == 0)
                        break;

                    Task taskQueueWaitingTask;
                    await Task.WhenAny(taskQueueWaitingTask = taskQueueSemaphore.WaitAsync(), executorFinishSignal.WaitAsync());
                    
                    taskQueueSemaphore.Release();
                    await taskQueueWaitingTask;
                    
                    if (taskQueueSemaphore.CurrentCount > 0)
                        break;
                }
            }
        }      
    }
}
