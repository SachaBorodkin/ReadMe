using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ReadMe.Models;
using ReadMe.Services;

namespace ReadMe.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService;
        private readonly BookApiService _apiService;
        private bool _isLoading;

        public ObservableCollection<Book> Books { get; set; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel(DatabaseService dbService, BookApiService apiService)
        {
            _dbService = dbService;
            _apiService = apiService;
            _ = LoadBooksAsync();
        }

        public async Task LoadBooksAsync()
        {
            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine("[MainViewModel] === Starting LoadBooksAsync ===");

                // 1. Fetch books from API
                System.Diagnostics.Debug.WriteLine("[MainViewModel] Calling API...");
                var apiBooks = await _apiService.FetchBooksFromApiAsync();
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] API returned {apiBooks?.Count ?? 0} books");

                if (apiBooks != null && apiBooks.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"First book: Title={apiBooks[0].Title}, Author={apiBooks[0].Author}");
                    
                    // 2. Clear existing database
                    await _dbService.DeleteAllBooksAsync();
                    System.Diagnostics.Debug.WriteLine("Cleared database");

                    // 3. Save API data to local DB
                    foreach (var book in apiBooks)
                    {
                        var result = await _dbService.SaveBookAsync(book);
                        System.Diagnostics.Debug.WriteLine($"Saved book '{book.Title}' with result: {result}");
                    }
                    System.Diagnostics.Debug.WriteLine($"Saved {apiBooks.Count} books to database");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No books received from API");
                }

                // 4. Load from DB into UI
                var items = await _dbService.GetBooksAsync();
                System.Diagnostics.Debug.WriteLine($"Loaded {items?.Count ?? 0} books from database");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Books.Clear();
                    foreach (var book in items)
                    {
                        Books.Add(book);
                        System.Diagnostics.Debug.WriteLine($"Added book: {book.Title}");
                    }
                    System.Diagnostics.Debug.WriteLine($"UI updated with {Books.Count} books");
                    IsLoading = false;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading books: {ex.Message}\n{ex.StackTrace}");
                IsLoading = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}