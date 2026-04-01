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

            // Define where the database file will live on the phone
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "OnlyBooks.db3");

            _database = new SQLiteAsyncConnection(dbPath);

            // Create the table if it doesn't exist
            await _database.CreateTableAsync<Book>();
        }

        public async Task<List<Book>> GetBooksAsync()
        {
            await Init();
            return await _database.Table<Book>().ToListAsync();
        }

        public async Task<int> SaveBookAsync(Book book)
        {
            await Init();
            if (book.Id != 0)
                return await _database.UpdateAsync(book);
            else
                return await _database.InsertAsync(book);
        }
    }
}