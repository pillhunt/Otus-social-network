using Microsoft.VisualStudio.Threading;
using snhw.Common;
using System.Collections.Concurrent;

namespace snhw.Workers
{
    public class PostingManager : BackgroundService
    {
        private CancellationToken cancellationToken;
        private readonly SemaphoreSlim postQueueSemaphore = Queues.PostingTaskQueueSemaphore;
        private readonly ConcurrentQueue<Task<IResult>> postingTaskQueue = Queues.PostingTaskQueue;

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Task<IResult> processedTask;
            AsyncManualResetEvent executorFinishSignal = new AsyncManualResetEvent();
            List<Task<IResult>> tasksWaitingList = new List<Task<IResult>>();
            this.cancellationToken = cancellationToken;

            while (!cancellationToken.IsCancellationRequested || !postingTaskQueue.IsEmpty) 
            {
                try
                {
                    await postQueueSemaphore.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    if (postingTaskQueue.IsEmpty)
                        break;
                }

                if (!postingTaskQueue.TryDequeue(out processedTask))
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

                    if (postQueueSemaphore.CurrentCount > 0 || tasksWaitingList.Count == 0)
                        break;

                    Task taskQueueWaitingTask;
                    await Task.WhenAny(taskQueueWaitingTask = postQueueSemaphore.WaitAsync(), executorFinishSignal.WaitAsync());

                    postQueueSemaphore.Release();
                    await taskQueueWaitingTask;

                    if (postQueueSemaphore.CurrentCount > 0)
                        break;
                }
            }
        }
    }
}
