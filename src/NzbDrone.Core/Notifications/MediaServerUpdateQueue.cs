using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Notifications
{
    public class MediaServerUpdateQueue<TQueueHost, TItemInfo>
        where TQueueHost : class
    {
        private class UpdateQueue
        {
            public Dictionary<string, UpdateQueueItem<TItemInfo>> Pending { get; } = new Dictionary<string, UpdateQueueItem<TItemInfo>>();
            public bool Refreshing { get; set; }
        }

        private readonly ICached<UpdateQueue> _pendingItemsCache;

        public MediaServerUpdateQueue(ICacheManager cacheManager)
        {
            _pendingItemsCache = cacheManager.GetRollingCache<UpdateQueue>(typeof(TQueueHost), "pendingItems", TimeSpan.FromDays(1));
        }

        public void Add(string identifier, Series series, TItemInfo info)
        {
            AddItem(identifier, "series:" + series.Id, () => new UpdateQueueItem<TItemInfo>(series), info);
        }

        public void Add(string identifier, Movie movie, TItemInfo info)
        {
            AddItem(identifier, "movie:" + movie.Id, () => new UpdateQueueItem<TItemInfo>(movie), info);
        }

        private void AddItem(string identifier, string key, Func<UpdateQueueItem<TItemInfo>> itemFactory, TItemInfo info)
        {
            var queue = _pendingItemsCache.Get(identifier, () => new UpdateQueue());

            lock (queue)
            {
                var item = queue.Pending.TryGetValue(key, out var value)
                    ? value
                    : itemFactory();

                item.Info.Add(info);

                queue.Pending[key] = item;
            }
        }

        public void ProcessQueue(string identifier, Action<List<UpdateQueueItem<TItemInfo>>> update)
        {
            var queue = _pendingItemsCache.Find(identifier);

            if (queue == null)
            {
                return;
            }

            lock (queue)
            {
                if (queue.Refreshing)
                {
                    return;
                }

                queue.Refreshing = true;
            }

            try
            {
                while (true)
                {
                    List<UpdateQueueItem<TItemInfo>> items;

                    lock (queue)
                    {
                        if (queue.Pending.Empty())
                        {
                            queue.Refreshing = false;
                            return;
                        }

                        items = queue.Pending.Values.ToList();
                        queue.Pending.Clear();
                    }

                    update(items);
                }
            }
            catch
            {
                lock (queue)
                {
                    queue.Refreshing = false;
                }

                throw;
            }
        }
    }

    public class UpdateQueueItem<TItemInfo>
    {
        public Series Series { get; set; }
        public Movie Movie { get; set; }
        public HashSet<TItemInfo> Info { get; set; }

        public UpdateQueueItem(Series series)
        {
            Series = series;
            Info = new HashSet<TItemInfo>();
        }

        public UpdateQueueItem(Movie movie)
        {
            Movie = movie;
            Info = new HashSet<TItemInfo>();
        }
    }
}
