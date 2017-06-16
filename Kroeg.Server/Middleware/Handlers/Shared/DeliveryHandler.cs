﻿using System.Threading.Tasks;
using Kroeg.Server.Models;
using Kroeg.Server.Services;
using Kroeg.Server.Services.EntityStore;

namespace Kroeg.Server.Middleware.Handlers.Shared
{
    public class DeliveryHandler : BaseHandler
    {
        private readonly CollectionTools _collection;
        private readonly DeliveryService _deliveryService;

        public DeliveryHandler(StagingEntityStore entityStore, APEntity mainObject, APEntity actor, APEntity targetBox, CollectionTools collection, DeliveryService deliveryService) : base(entityStore, mainObject, actor, targetBox)
        {
            _collection = collection;
            _deliveryService = deliveryService;
        }

        public override async Task<bool> Handle()
        {
            var addedTo = await _collection.AddToCollection(TargetBox, MainObject);
            if (MainObject.Type == "Block") return true;

            await _deliveryService.QueueDeliveryForEntity(MainObject, addedTo.CollectionItemId, TargetBox.Type == "_inbox");
            return true;
        }
    }
}