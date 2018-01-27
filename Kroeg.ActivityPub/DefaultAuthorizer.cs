using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kroeg.ActivityPub.Services;
using Kroeg.EntityStore.Models;
using Kroeg.EntityStore.Services;
using Kroeg.Services;

namespace Kroeg.ActivityPub
{
    public class DefaultAuthorizer : IAuthorizer
    {
        private readonly ServerConfig _entityData;

        public DefaultAuthorizer(ServerConfig entityData)
        {
            _entityData = entityData;
        }

        private HashSet<string> _probablyVisible = new HashSet<string>
        {
            "https://www.w3.org/ns/activitystreams#OrderedCollection",
            "https://www.w3.org/ns/activitystreams#OrderedCollectionPage",
            "https://www.w3.org/ns/activitystreams#Collection",
            "https://www.w3.org/ns/activitystreams#CollectionPage"
        };

        public async Task<bool> VerifyAccess(APEntity entity, string userId)
        {
            if (entity.Type == "_blocks" && !entity.Data["attributedTo"].Any(a => a.Id == userId)) return false;
            if (entity.Type == "_blocked") return false;
            if (_probablyVisible.Contains(entity.Type) || entity.Type.StartsWith("_")) return true;
            if (EntityData.IsActor(entity.Data) || entity.Type == "https://puckipedia.com/kroeg/ns#Server") return true;

            var audience = DeliveryService.GetAudienceIds(entity.Data);
            return (
                audience.Count == 0
                || entity.Data["attributedTo"].Concat(entity.Data["actor"]).Any(a => a.Id == userId)
                || audience.Contains("https://www.w3.org/ns/activitystreams#Public")
                || (userId != null  && audience.Contains(userId))
                );
        }
    }
}
