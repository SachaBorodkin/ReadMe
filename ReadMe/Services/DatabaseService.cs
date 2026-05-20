using SQLite;
using ReadMe.Models;

namespace ReadMe.Services
{
    public class DatabaseService
    {
        SQLiteAsyncConnection _database;

        async Task Init()
        {
            if (_database is not null)
                return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "api.db");
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] Initializing database at: {dbPath}");

            _database = new SQLiteAsyncConnection(dbPath);

            await _database.CreateTableAsync<Book>();
            await _database.CreateTableAsync<Tag>();
            await _database.CreateTableAsync<BookTag>();
            System.Diagnostics.Debug.WriteLine("[DatabaseService] Database initialized and tables created");
        }

        public async Task<List<Book>> GetBooksAsync()
        {
            await Init();
            var books = await _database.Table<Book>().ToListAsync();
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] Retrieved {books.Count} books from database");
            foreach (var book in books)
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] Book: {book.Title}");
            return books;
        }

        public async Task<int> SaveBookAsync(Book book)
        {
            await Init();
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] Saving book: {book.Title} with ID: {book.Id}");
            try
            {

                Book existing = null;
                if (!string.IsNullOrEmpty(book.EpubFilePath))
                {
                    existing = await _database.Table<Book>().Where(b => b.EpubFilePath == book.EpubFilePath).FirstOrDefaultAsync();
                }

                if (existing == null && book.Id != 0)
                {
                    existing = await _database.FindAsync<Book>(book.Id);
                }

                if (existing == null)
                {
                    var result = await _database.InsertAsync(book);
                    System.Diagnostics.Debug.WriteLine($"[DatabaseService] Insert result: {result}");
                    return result;
                }
                else
                {
                    book.Id = existing.Id;
                    var result = await _database.UpdateAsync(book);
                    System.Diagnostics.Debug.WriteLine($"[DatabaseService] Update result: {result}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] Error saving book: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> DeleteAllBooksAsync()
        {
            await Init();
            var count = await _database.DeleteAllAsync<Book>();
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] Deleted {count} books from database");
            return count;
        }

        public async Task<int> DeleteBookAsync(int bookId)
        {
            await Init();
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] Deleting book with ID: {bookId}");
            try
            {
                var result = await _database.DeleteAsync<Book>(bookId);
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] Delete result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] Error deleting book: {ex.Message}");
                return 0;
            }
        }

        public async Task<List<Tag>> GetAllTagsAsync()
        {
            await Init();
            return await _database.Table<Tag>().ToListAsync();
        }

        public async Task<Tag> GetOrCreateTagAsync(string name)
        {
            await Init();
            var tag = await _database.Table<Tag>().Where(t => t.Name == name).FirstOrDefaultAsync();
            if (tag == null)
            {
                tag = new Tag { Name = name };
                await _database.InsertAsync(tag);
            }
            return tag;
        }

        public async Task<List<int>> GetBookIdsForTagAsync(string tagName)
        {
            await Init();
            var tag = await _database.Table<Tag>().Where(t => t.Name == tagName).FirstOrDefaultAsync();
            if (tag == null) return new List<int>();

            var links = await _database.Table<BookTag>().Where(bt => bt.TagId == tag.Id).ToListAsync();
            return links.Select(bt => bt.BookId).ToList();
        }

        public async Task<List<Tag>> GetTagsForBookAsync(int bookId)
        {
            await Init();
            var links = await _database.Table<BookTag>().Where(bt => bt.BookId == bookId).ToListAsync();
            var tagIds = links.Select(bt => bt.TagId).ToList();

            var tags = new List<Tag>();
            foreach (var id in tagIds)
            {
                var t = await _database.FindAsync<Tag>(id);
                if (t != null) tags.Add(t);
            }
            return tags;
        }

        public async Task SetTagsForBookAsync(int bookId, List<string> tagNames)
        {
            await Init();
            
            // Delete existing
            var existingLinks = await _database.Table<BookTag>().Where(bt => bt.BookId == bookId).ToListAsync();
            foreach (var link in existingLinks)
            {
                await _database.DeleteAsync(link);
            }

            // Insert new
            foreach (var tagName in tagNames)
            {
                var tag = await GetOrCreateTagAsync(tagName);
                await _database.InsertAsync(new BookTag { BookId = bookId, TagId = tag.Id });
            }
        }
    }
}
