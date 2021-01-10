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

        public void TryEnqueue(DateTime obj)
        {
            RemoveStaleEntries();

            if (IsFull())
            {
                return;
            }

            q.Enqueue(obj);
        }

        public int Count => q.Count;

        public bool IsFull() => q.Count >= _limit;

        public DateTime Oldest() => q.TryPeek(out var d) ? d : DateTime.UtcNow;

        public void RemoveStaleEntries()
        {
            lock (lockObject)
            {
                while (q.TryPeek(out var element) && (DateTime.UtcNow - element) > _lifetime)
                {
                    q.TryDequeue(out _);
                };
            }
        }
    }
}