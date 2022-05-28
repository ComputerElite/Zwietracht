using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zwietracht.Models
{
    [BsonIgnoreExtraElements]
    public class User
    {
        public string nickname { get; set; } = "";
        public string id { get; set; } = DateTime.UtcNow.Ticks.ToString();
        [BsonIgnore]
        public List<Channel> channels { get; set; } = new List<Channel>(); // Ignore as those are set by server
    }
}
