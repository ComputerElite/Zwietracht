namespace Zwietracht.Models
{
    public class Attachment
    {
        public string url { get; set; } = "";
        public string relUrl { get; set; } = "";
        public string filename { get; set; } = "";
        public string mimeType { get; set; } = "";
        public string base64 { get; set; } = ""; // Only on post
    }
}