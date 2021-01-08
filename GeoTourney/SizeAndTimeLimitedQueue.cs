using System;
using System.Collections.Concurrent;

namespace GeoTourney
{
    public class SizeAndTimeLimitedQueue
    {
        readonly TimeSpan _lifetime;
        readonly int _limit;
        readonly object lockObject = new();
        readonly ConcurrentQueue<DateTime> q = new();

        public SizeAndTimeLimitedQueue(TimeSpan lifetime, int limit)
        {
            _lifetime = lifetime;
            _limit = limit;
        }

        public bool TryEnqueue(DateTime obj)
        {
            lock (lockObject)
            {
                while (q.TryPeek(out var element) && (DateTime.UtcNow - element) > _lifetime)
                {
                    q.TryDequeue(out _);
                };
            }

            if (q.Count < _limit)
            {
                q.Enqueue(obj);
                return true;
            }

            return false;
        }

        public int Count => q.Count;

        public DateTime Oldest() => q.TryPeek(out var d) ? d : DateTime.UtcNow;
    }
}