using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zwietracht.Models
{
    [BsonIgnoreExtraElements]
    public class UserInfo
    {
        public string nickname { get; set; } = "";
        public string id { get; set; } = DateTime.UtcNow.Ticks.ToString();
        public string passwordSHA256 { get; set; } = "";
        public string passwordSalt { get; set; } = "";
        public string currentTokenSHA256 { get; set; } = "";
    }
}
