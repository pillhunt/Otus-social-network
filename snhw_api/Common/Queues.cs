using System.Collections.Concurrent;

namespace SocialnetworkHomework.Common
{
    internal static class Queues
    {
        internal static SemaphoreSlim RequestTaskQueueSemaphore { get; set; } = new SemaphoreSlim(0, 100);
        internal static ConcurrentQueue<Task<IResult>> RequestTaskQueue { get; set; } = new ConcurrentQueue<Task<IResult>>();
    }
}
