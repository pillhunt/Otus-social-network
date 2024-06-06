
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net;

namespace SocialnetworkHomework.workers
{
    internal sealed class RequestManager : BackgroundService
    {
        private CancellationToken cancellationToken;

        private readonly SemaphoreSlim _taskQueueSemaphore;
        private readonly ConcurrentQueue<Task<IResult>> _taskQueue;

        public RequestManager(RequestActions requestActions) 
        {
            _taskQueueSemaphore = requestActions.taskQueueSemaphore;
            _taskQueue = requestActions.requestTaskQueue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task<IResult> processedTask;

            this.cancellationToken = cancellationToken;
            AsyncManualResetEvent executorFinishSignal = new AsyncManualResetEvent();
            var tasksWaitingList = new List<Task<IResult>>();

            while (!cancellationToken.IsCancellationRequested || !_taskQueue.IsEmpty)
            {
                try
                {
                    await _taskQueueSemaphore.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    if (_taskQueue.IsEmpty)
                        break;
                }

                if (!_taskQueue.TryDequeue(out processedTask))
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

                        _ = taskInWaitingList.WaitAsync(cancellationToken);
                        tasksWaitingList.Remove(taskInWaitingList);
                    }

                    if (_taskQueueSemaphore.CurrentCount > 0 || tasksWaitingList.Count == 0)
                        break;

                    Task taskQueueWaitingTask;
                    await Task.WhenAny(taskQueueWaitingTask = _taskQueueSemaphore.WaitAsync(), executorFinishSignal.WaitAsync());
                    _taskQueueSemaphore.Release();
                    await taskQueueWaitingTask;
                    if (_taskQueueSemaphore.CurrentCount > 0)
                        break;
                }                    
            }
        }      
    }
}
