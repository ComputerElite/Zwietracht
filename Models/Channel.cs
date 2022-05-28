using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zwietracht.Models
{
    [BsonIgnoreExtraElements]
    public class Channel
    {
        public string id { get; set; } = DateTime.UtcNow.Ticks.ToString();
        public List<User> participants { get; set; } = new List<User>();
    }
}
