using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Kroeg.ActivityStreams;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Kroeg.EntityStore.Models
{
    public class APTripleEntity
    {
        public int IdId { get; set; }
        public TripleAttribute Id { get; set; }

        [Key]
        public int EntityId { get; set; }

        public string Type { get; set; }

        public DateTime Updated { get; set; }

        public int? IsOwner { get; set; }
    }
}
