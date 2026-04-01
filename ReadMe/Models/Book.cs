using SQLite;

namespace ReadMe.Models
{
    public class Book
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Title { get; set; }
        public string Author { get; set; }
        public string CoverImage { get; set; } // Path to the image
        public int TotalPages { get; set; }
        public int LastPageOpened { get; set; }
        public DateTime LastOpenedDate { get; set; }

        // We can calculate the progress percentage
        [Ignore]
        public double Progress => TotalPages > 0 ? (double)LastPageOpened / TotalPages : 0;
    }
}