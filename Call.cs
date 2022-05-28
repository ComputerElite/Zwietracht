using Zwietracht.Models;

namespace Zwietracht
{
    public class Call
    {
        public Channel channel { get; set; } = new Channel();
        public List<AudioChunk> chunks { get; set; } = new List<AudioChunk>();
    }

    public class AudioChunk
    {
        public string base64 { get; set; } = "";
        public string userId { get; set; } = "";
    }
}