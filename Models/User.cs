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
    public class User
    {
        public string nickname { get; set; } = "";
        public string id
        {
            get
            {
                return idLong.ToString();
            }
            set
            {
                idLong = Convert.ToInt64(value);
            }
        }
        [JsonIgnore]
        public long idLong { get; set; } = DateTime.UtcNow.Ticks;
        public List<Channel> channels { get; set; } = new List<Channel>();
    }
}
