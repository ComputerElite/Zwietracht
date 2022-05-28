using ComputerUtils.Webserver;
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
        public DateTime sent { get; set; } = DateTime.Now;
        public Client client { get; set; } = new Client();
    }

    public class Client
    {
        public SocketServerRequest request { get; set; } = null;
        public DateTime recieved { get; set; } = DateTime.Now;
        public string userId { get; set; } = "";
    }
}