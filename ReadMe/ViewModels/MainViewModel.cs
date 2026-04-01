using System.Collections.ObjectModel;
using ReadMe.Models;   // Add this
using ReadMe.Services;

namespace ReadMe.ViewModels
{
    public class MainViewModel
    {
        private readonly DatabaseService _dbService;

        // This is the list your XAML will talk to
        public ObservableCollection<Book> Books { get; set; } = new();

        public MainViewModel(DatabaseService dbService)
        {
            _dbService = dbService;
            LoadBooks();
        }

        public async void LoadBooks()
        {
            var items = await _dbService.GetBooksAsync();

            // If DB is empty, let's add some "Seed" data for testing
            if (items.Count == 0)
            {
                await _dbService.SaveBookAsync(new Book { Title = "67 Méthodes Agiles", Author = "Anass Benfares", TotalPages = 367, LastPageOpened = 67 });
                await _dbService.SaveBookAsync(new Book { Title = "Histoire du business", Author = "James Captur", TotalPages = 319, LastPageOpened = 99 });
                items = await _dbService.GetBooksAsync();
            }

            Books.Clear();
            foreach (var book in items)
                Books.Add(book);
        }
    }
}