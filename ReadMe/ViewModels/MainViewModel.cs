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
        private readonly List<Book> _allBooks = new();
        private readonly List<TagItem> _allTags = new();
        private readonly List<BookSelectionItem> _tagPickerSourceBooks = new();
        private readonly Dictionary<string, HashSet<int>> _tagAssignments = new(StringComparer.OrdinalIgnoreCase);
        private bool _isLoading;
        private bool _isBooksView = true;
        private bool _isFiltersVisible;
        private string _searchText = string.Empty;
        private string _selectedSortOption = "Date d'ajout";
        private string _selectedTagFilter = "Tous les tags";
        private bool _isTagPickerVisible;
        private TagItem _selectedTag;
        private string _tagPickerSearchText = string.Empty;

        public ObservableCollection<Book> Books { get; } = new();
        public ObservableCollection<TagItem> Tags { get; } = new();
        public ObservableCollection<string> SortOptions { get; } = new() { "Date d'ajout", "Titre", "Auteur" };
        public ObservableCollection<string> TagFilterOptions { get; } = new() { "Tous les tags" };
        public ObservableCollection<BookSelectionItem> TagPickerBooks { get; } = new();

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

        public bool IsBooksView
        {
            get => _isBooksView;
            set
            {
                if (_isBooksView != value)
                {
                    _isBooksView = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentSectionTitle));
                    OnPropertyChanged(nameof(SearchPlaceholder));
                    OnPropertyChanged(nameof(IsTagsView));
                    RefreshVisibleCollections();
                }
            }
        }

        public bool IsTagsView => !IsBooksView;

        public bool IsFiltersVisible
        {
            get => _isFiltersVisible;
            set
            {
                if (_isFiltersVisible != value)
                {
                    _isFiltersVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsTagPickerVisible
        {
            get => _isTagPickerVisible;
            set
            {
                if (_isTagPickerVisible != value)
                {
                    _isTagPickerVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentSectionTitle => IsBooksView ? "Derniers livres" : "Tags";

        public string SearchPlaceholder => IsBooksView ? "Chercher un livre" : "Chercher un tag";

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value ?? string.Empty;
                    OnPropertyChanged();
                    RefreshVisibleCollections();
                }
            }
        }

        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (_selectedSortOption != value)
                {
                    _selectedSortOption = value;
                    OnPropertyChanged();
                    RefreshVisibleBooks();
                }
            }
        }

        public string SelectedTagFilter
        {
            get => _selectedTagFilter;
            set
            {
                if (_selectedTagFilter != value)
                {
                    _selectedTagFilter = value;
                    OnPropertyChanged();
                    RefreshVisibleBooks();
                }
            }
        }

        public TagItem SelectedTag
        {
            get => _selectedTag;
            set
            {
                if (_selectedTag != value)
                {
                    _selectedTag = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TagPickerTitle));
                }
            }
        }

        // --- Add Book Properties ---
        private bool _isAddBookVisible;
        private string _newBookTitle;
        private string _newBookAuthor;
        private string _newBookEpubPath;
        private string _newBookTag;

        public bool IsAddBookVisible
        {
            get => _isAddBookVisible;
            set { if (_isAddBookVisible != value) { _isAddBookVisible = value; OnPropertyChanged(); } }
        }

        public string NewBookTitle
        {
            get => _newBookTitle;
            set { if (_newBookTitle != value) { _newBookTitle = value; OnPropertyChanged(); } }
        }

        public string NewBookAuthor
        {
            get => _newBookAuthor;
            set { if (_newBookAuthor != value) { _newBookAuthor = value; OnPropertyChanged(); } }
        }

        public string NewBookEpubPath
        {
            get => _newBookEpubPath;
            set { if (_newBookEpubPath != value) { _newBookEpubPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(NewBookEpubFileName)); } }
        }

        public string NewBookEpubFileName => string.IsNullOrEmpty(_newBookEpubPath) ? "Aucun fichier sélectionné" : Path.GetFileName(_newBookEpubPath);

        public string NewBookTag
        {
            get => _newBookTag;
            set { if (_newBookTag != value) { _newBookTag = value; OnPropertyChanged(); } }
        }
        // --- Add Book Properties End ---

        public string TagPickerSearchText
        {
            get => _tagPickerSearchText;
            set
            {
                if (_tagPickerSearchText != value)
                {
                    _tagPickerSearchText = value ?? string.Empty;
                    OnPropertyChanged();
                    RefreshTagPickerBooks();
                }
            }
        }

        public string TagPickerTitle => SelectedTag == null ? "Associer un livre" : $"Associer à {SelectedTag.Name}";

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
                    foreach (var book in localBooks)
                    {
                        var existing = existingDbItems.FirstOrDefault(b => !string.IsNullOrEmpty(b.EpubFilePath) && b.EpubFilePath == book.EpubFilePath);
                        if (existing != null)
                        {
                            book.Id = existing.Id;
                            book.LastPageOpened = existing.LastPageOpened;
                            book.LastOpenedDate = existing.LastOpenedDate;
                        }

                        await _dbService.SaveBookAsync(book);
                    }
                }

                var items = await _dbService.GetBooksAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _allBooks.Clear();
                    _allBooks.AddRange(items);

                    EnsureDefaultTags();
                    RefreshVisibleCollections();
                    IsLoading = false;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading books: {ex.Message}\n{ex.StackTrace}");
                IsLoading = false;
            }
        }

        public void SetBooksView()
        {
            IsBooksView = true;
        }

        public void SetTagsView()
        {
            IsBooksView = false;
        }

        public void ToggleFilters()
        {
            IsFiltersVisible = !IsFiltersVisible;
        }

        public void OpenTagPicker(TagItem tag)
        {
            if (tag == null)
                return;

            SelectedTag = tag;
            TagPickerSearchText = string.Empty;
            _tagPickerSourceBooks.Clear();
            TagPickerBooks.Clear();

            var selectedIds = GetTagAssignments(tag.Name);
            foreach (var book in _allBooks)
            {
                var item = new BookSelectionItem(book)
                {
                    IsSelected = selectedIds.Contains(book.Id)
                };

                _tagPickerSourceBooks.Add(item);
                TagPickerBooks.Add(item);
            }

            IsTagPickerVisible = true;
        }

        public void CloseTagPicker()
        {
            IsTagPickerVisible = false;
            SelectedTag = null;
            TagPickerBooks.Clear();
        }

        // --- Add Book Methods ---
        public void OpenAddBook()
        {
            NewBookTitle = string.Empty;
            NewBookAuthor = string.Empty;
            NewBookEpubPath = string.Empty;
            NewBookTag = string.Empty;
            IsAddBookVisible = true;
        }

        public void CloseAddBook()
        {
            IsAddBookVisible = false;
        }

        public async Task PickEpubAsync()
        {
            try
            {
                var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "org.idpf.epub-container" } },
                    { DevicePlatform.Android, new[] { "application/epub+zip" } },
                    { DevicePlatform.WinUI, new[] { ".epub" } },
                    { DevicePlatform.MacCatalyst, new[] { "org.idpf.epub-container" } }
                });
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Sélectionner un fichier EPUB",
                    FileTypes = customFileType
                });

                if (result != null)
                {
                    // Fix: Check the actual bound property value, as an empty entry sometimes returns whitespace
                    if (string.IsNullOrWhiteSpace(NewBookTitle))
                    {
                        NewBookTitle = Path.GetFileNameWithoutExtension(result.FileName);
                    }
                    NewBookEpubPath = result.FullPath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Error picking file: {ex.Message}");
            }
        }

        public async Task ConfirmAddBookAsync()
        {
            if (string.IsNullOrWhiteSpace(NewBookTitle) || string.IsNullOrWhiteSpace(NewBookAuthor) || string.IsNullOrWhiteSpace(NewBookEpubPath))
                return;

            try
            {
                IsLoading = true;
                IsAddBookVisible = false;

                var finalTitle = NewBookTitle?.Trim() ?? "Titre Inconnu";
                var finalAuthor = NewBookAuthor?.Trim() ?? "Auteur Inconnu";

                var newBook = await _localBooksService.AddUserBookAsync(NewBookEpubPath, finalAuthor, finalTitle);
                if (newBook != null)
                {
                    await _dbService.SaveBookAsync(newBook);
                    _allBooks.Add(newBook);

                    if (!string.IsNullOrWhiteSpace(NewBookTag))
                    {
                        var tagToFind = NewBookTag.Trim();
                        var existingTag = _allTags.FirstOrDefault(t => t.Name.Equals(tagToFind, StringComparison.OrdinalIgnoreCase));
                        if (existingTag == null)
                        {
                            existingTag = new TagItem(tagToFind);
                            _allTags.Add(existingTag);
                        }
                        
                        if (!_tagAssignments.ContainsKey(existingTag.Name))
                        {
                            _tagAssignments[existingTag.Name] = new HashSet<int>();
                        }
                        _tagAssignments[existingTag.Name].Add(newBook.Id);
                    }

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ApplyTagCounts();
                        RefreshTagFilterOptions();
                        RefreshVisibleCollections();
                    });
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
        // --- Add Book Methods End ---

        public void ConfirmTagPickerSelection()
        {
            if (SelectedTag == null)
                return;

            var selectedIds = _tagPickerSourceBooks.Where(item => item.IsSelected).Select(item => item.Book.Id).ToHashSet();
            _tagAssignments[SelectedTag.Name] = selectedIds;

            ApplyTagCounts();
            CloseTagPicker();
            RefreshVisibleCollections();
        }

        private void EnsureDefaultTags()
        {
            if (_allTags.Count == 0)
            {
                _allTags.Add(new TagItem("Classiques"));
                _allTags.Add(new TagItem("Aventure"));
                _allTags.Add(new TagItem("Voyage"));
                _allTags.Add(new TagItem("Lecture"));
                _allTags.Add(new TagItem("Favoris"));

                SeedTagAssignments();
            }

            ApplyTagCounts();
            RefreshTagFilterOptions();
        }

        private void SeedTagAssignments()
        {
            if (_allBooks.Count == 0)
                return;

            _tagAssignments["Classiques"] = _allBooks.Select(book => book.Id).ToHashSet();
            _tagAssignments["Aventure"] = _allBooks.Where(book => book.Author.Contains("Verne", StringComparison.OrdinalIgnoreCase) || book.Author.Contains("Dumas", StringComparison.OrdinalIgnoreCase) || book.Author.Contains("Doyle", StringComparison.OrdinalIgnoreCase)).Select(book => book.Id).ToHashSet();
            _tagAssignments["Voyage"] = _allBooks.Where(book => book.Title.Contains("tour du monde", StringComparison.OrdinalIgnoreCase) || book.Title.Contains("Voyage", StringComparison.OrdinalIgnoreCase)).Select(book => book.Id).ToHashSet();
            _tagAssignments["Lecture"] = _allBooks.Where(book => book.Title.Contains("Twist", StringComparison.OrdinalIgnoreCase) || book.Title.Contains("Carol", StringComparison.OrdinalIgnoreCase)).Select(book => book.Id).ToHashSet();
            _tagAssignments["Favoris"] = new HashSet<int>(_allBooks.Take(2).Select(book => book.Id));
        }

        private void ApplyTagCounts()
        {
            foreach (var tag in _allTags)
            {
                tag.SetAssociatedBooks(_allBooks.Where(book => GetTagAssignments(tag.Name).Contains(book.Id)));
            }
        }

        private void RefreshVisibleCollections()
        {
            RefreshVisibleBooks();
            RefreshVisibleTags();
            RefreshTagPickerBooks();
        }

        private void RefreshVisibleBooks()
        {
            IEnumerable<Book> query = _allBooks;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(book =>
                    (book.Title?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (book.Author?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (book.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(SelectedTagFilter) && SelectedTagFilter != "Tous les tags")
            {
                var ids = GetTagAssignments(SelectedTagFilter);
                query = query.Where(book => ids.Contains(book.Id));
            }

            query = SelectedSortOption switch
            {
                "Titre" => query.OrderBy(book => book.Title),
                "Auteur" => query.OrderBy(book => book.Author),
                _ => query.OrderByDescending(book => book.LastOpenedDate)
            };

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Books.Clear();
                foreach (var book in query)
                {
                    Books.Add(book);
                }
            });
        }

        private void RefreshVisibleTags()
        {
            if (!IsTagsView && string.IsNullOrWhiteSpace(SearchText))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Tags.Clear();
                    foreach (var tag in _allTags)
                    {
                        Tags.Add(tag);
                    }
                });
                return;
            }

            IEnumerable<TagItem> query = _allTags;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(tag => tag.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Tags.Clear();
                foreach (var tag in query)
                {
                    Tags.Add(tag);
                }
            });
        }

        private void RefreshTagPickerBooks()
        {
            if (!IsTagPickerVisible)
                return;

            IEnumerable<BookSelectionItem> query = _tagPickerSourceBooks;

            if (!string.IsNullOrWhiteSpace(TagPickerSearchText))
            {
                query = query.Where(item =>
                    (item.Title?.Contains(TagPickerSearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (item.Author?.Contains(TagPickerSearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TagPickerBooks.Clear();
                foreach (var item in query)
                {
                    TagPickerBooks.Add(item);
                }
            });
        }

        private void RefreshTagFilterOptions()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TagFilterOptions.Clear();
                TagFilterOptions.Add("Tous les tags");

                foreach (var tag in _allTags)
                {
                    TagFilterOptions.Add(tag.Name);
                }

                if (!TagFilterOptions.Contains(SelectedTagFilter))
                {
                    SelectedTagFilter = "Tous les tags";
                }
            });
        }

        private HashSet<int> GetTagAssignments(string tagName)
        {
            if (_tagAssignments.TryGetValue(tagName, out var ids))
            {
                return ids;
            }

            return new HashSet<int>();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
