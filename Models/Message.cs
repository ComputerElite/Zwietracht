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
    public class Message
    {
        public string id
        {
            get
            {
                return idLong.ToString();
            }
        }
        [JsonIgnore]
        public long idLong { get; set; } = 0;
        public User author { get; set; } = new User();
        public List<Attachment> attachments { get; set; } = new List<Attachment>();
        public string content { get; set; } = "";
        public DateTime sendTime { get; set; } = DateTime.Now;
    }
}
