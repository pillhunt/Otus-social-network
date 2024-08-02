using SocialnetworkHomework.Data;
using System.Collections.Concurrent;

namespace SocialnetworkHomework.Common
{
    internal static class Queues
    {
        internal static SemaphoreSlim RequestTaskQueueSemaphore { get; set; } = new SemaphoreSlim(0, 100);
        internal static ConcurrentQueue<Task<IResult>> RequestTaskQueue { get; set; } = new ConcurrentQueue<Task<IResult>>();
        internal static Queue<PostingPerson> ReadyForPostingPerson { get; set; }  = new Queue<PostingPerson>();        
        internal static List<PostingPerson> PostingPersonsList { get; set; } = new List<PostingPerson>();
    }
}
