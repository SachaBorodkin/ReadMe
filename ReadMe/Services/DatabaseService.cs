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
            System.Diagnostics.Debug.WriteLine("[DatabaseService] Database initialized and table created");
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
    }
}
