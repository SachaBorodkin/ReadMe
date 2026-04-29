using SQLite;
using System.Text.Json.Serialization;

namespace ReadMe.Models
{
    public class Book
    {
        [PrimaryKey]
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("cover_image_path")]
        public string CoverImage { get; set; }

        [JsonPropertyName("file_size_bytes")]
        public int TotalPages { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("epub_file_path")]
        public string EpubFilePath { get; set; }

        [JsonPropertyName("isbn")]
        public string Isbn { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("publish_date")]
        public string PublishDate { get; set; }

        [JsonPropertyName("uploaded_at")]
        public string UploadedAt { get; set; }

        public int LastPageOpened { get; set; }

        public DateTime LastOpenedDate { get; set; }

        [Ignore]
        public double Progress => TotalPages > 0 ? (double)LastPageOpened / TotalPages : 0;
    }
}
