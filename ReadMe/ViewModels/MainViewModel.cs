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
        private readonly LocalBooksService _localBooksService;
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

        public MainViewModel(DatabaseService dbService, LocalBooksService localBooksService)
        {
            _dbService = dbService;
            _localBooksService = localBooksService;
            _ = LoadBooksAsync();
        }

        public async Task LoadBooksAsync()
        {
            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine("[MainViewModel] === Starting LoadBooksAsync ===");

                System.Diagnostics.Debug.WriteLine("[MainViewModel] Loading local books...");
                var localBooks = await _localBooksService.LoadLocalBooksAsync();
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Local books returned {localBooks?.Count ?? 0} books");

                var existingDbItems = await _dbService.GetBooksAsync();

                if (localBooks != null && localBooks.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"First book: Title={localBooks[0].Title}, Author={localBooks[0].Author}");

                    foreach (var book in localBooks)
                    {
                        var existing = existingDbItems.FirstOrDefault(b => !string.IsNullOrEmpty(b.EpubFilePath) && b.EpubFilePath == book.EpubFilePath);
                        if (existing != null)
                        {
                            book.Id = existing.Id;
                            book.LastPageOpened = existing.LastPageOpened;
                            book.LastOpenedDate = existing.LastOpenedDate;
                        }

                        var result = await _dbService.SaveBookAsync(book);
                        System.Diagnostics.Debug.WriteLine($"Saved/Updated book '{book.Title}' with result: {result}");
                    }
                    System.Diagnostics.Debug.WriteLine($"Processed {localBooks.Count} local books into database");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No local books found");
                }

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
