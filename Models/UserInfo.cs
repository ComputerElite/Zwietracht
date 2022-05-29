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
    public class UserInfo
    {
        public string nickname { get; set; } = "";
        public string id
        {
            get
            {
                return idLong.ToString();
            }
        }
        [JsonIgnore]
        public long idLong { get; set; } = DateTime.UtcNow.Ticks;
        public string passwordSHA256 { get; set; } = "";
        public string passwordSalt { get; set; } = "";
        public string currentTokenSHA256 { get; set; } = "";
        public List<Channel> channels { get; set; } = new List<Channel>();
    }
}
