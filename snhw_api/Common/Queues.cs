using snhw_api.Data;
using System.Collections.Concurrent;

namespace snhw_api.Common
{
    internal static class Queues
    {
        internal static SemaphoreSlim RequestTaskQueueSemaphore { get; set; } = new SemaphoreSlim(0, 100);
        internal static SemaphoreSlim PostingTaskQueueSemaphore { get; set; } = new SemaphoreSlim(0, 100);
        internal static ConcurrentQueue<Task<IResult>> RequestTaskQueue { get; set; } = new ConcurrentQueue<Task<IResult>>();
        internal static ConcurrentQueue<Task<IResult>> PostingTaskQueue { get; set; } = new ConcurrentQueue<Task<IResult>>();
        internal static List<PostingPerson> ReadyForPostingPerson { get; set; }  = new List<PostingPerson>(1000);        
        internal static List<PostingPerson> PostingPersonsList { get; set; } = new List<PostingPerson>(1000);
    }
}
