namespace ImageService
{
    public class ImageModel
    {
        public int Id { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public int Source { get; set; }
    }
}