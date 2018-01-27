using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kroeg.EntityStore.Models;
using Kroeg.EntityStore.Store;
using Kroeg.EntityStore;

namespace Kroeg.ActivityPub.BackgroundTasks
{
    public class GetEntityTask : BaseTask<string, GetEntityTask>
    {
        private readonly IEntityStore _entityStore;

        public GetEntityTask(EventQueueItem item, IEntityStore entityStore) : base(item)
        {
            _entityStore = entityStore;
        }

        public override async Task Go()
        {
            var queue = new Queue<string>();
            queue.Enqueue(Data);
            try
            {
                while (queue.Count > 0)
                {
                    var entity = await _entityStore.GetEntity(queue.Dequeue(), true);
                    if (entity == null) continue;
                    foreach (var item in entity.Data["inReplyTo"]) if (item.Id != null) queue.Enqueue(item.Id);
                    foreach (var item in entity.Data["actor"]) if (item.Id != null) queue.Enqueue(item.Id);
                    foreach (var item in entity.Data["attributedTo"]) if (item.Id != null) queue.Enqueue(item.Id);
                }
            } catch (Exception) { /* nom */ }
        }
    }
}
