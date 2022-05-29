using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Zwietracht.Models
{
    [BsonIgnoreExtraElements]
    public class Channel
    {
        public string id
        {
            get
            {
                return idLong.ToString();
            }
        }
        [JsonIgnore]
        public long idLong { get; set; } = DateTime.UtcNow.Ticks;
        public List<User> participants { get; set; } = new List<User>();
        [BsonIgnore]
        public string lastRead
        {
            get
            {
                return lastReadLong.ToString();
            }
        }
        [JsonIgnore]
        public long lastReadLong { get; set; } = 0;
        public long unread { get; set; } = 0;
    }
}
