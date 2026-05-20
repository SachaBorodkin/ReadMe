using SQLite;

namespace ReadMe.Models
{
    public class BookTag
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int BookId { get; set; }

        [Indexed]
        public int TagId { get; set; }
    }
}
