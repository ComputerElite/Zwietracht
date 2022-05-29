using ComputerUtils.Webserver;
using System.Text.Json.Serialization;
using Zwietracht.Models;

namespace Zwietracht
{
    public class Call
    {
        public Channel channel { get; set; } = new Channel();
        public List<Client> clients { get; set; } = new List<Client>();
    }

    public class AudioChunk
    {
        public string base64 { get; set; } = "";
        public string userId { get; set; } = "";
        public DateTime sent { get; set; } = DateTime.UtcNow;
        public Client client { get; set; } = new Client();
    }

    public class Client
    {
        [JsonIgnore]
        public SocketServerRequest request { get; set; } = null;
        public DateTime received { get; set; } = DateTime.UtcNow;
        public string userId { get; set; } = "";
    }
}