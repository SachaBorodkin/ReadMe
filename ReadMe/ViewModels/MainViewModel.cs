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
        private readonly BookApiService _bookApiService;
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
        private TagItem _selectedTagForBookFilter;
        private bool _isViewingTagBooks;

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

        public string CurrentSectionTitle => IsViewingTagBooks ? $"Livres - {_selectedTagForBookFilter?.Name}" : (IsBooksView ? "Derniers livres" : "Tags");

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
        private string _newBookTitle = string.Empty;
        private string _newBookAuthor = string.Empty;
        private string _newBookEpubPath = string.Empty;
        private string _newBookTag = string.Empty;

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

        // --- Create Tag Properties ---
        private bool _isCreateTagVisible;
        private string _newTagName = string.Empty;

        public bool IsCreateTagVisible
        {
            get => _isCreateTagVisible;
            set { if (_isCreateTagVisible != value) { _isCreateTagVisible = value; OnPropertyChanged(); } }
        }

        public string NewTagName
        {
            get => _newTagName;
            set { if (_newTagName != value) { _newTagName = value; OnPropertyChanged(); } }
        }
        
        // --- Book Tag Picker Properties ---
        private bool _isBookTagPickerVisible;
        private string _bookTagPickerSearchText = string.Empty;
        private Book _selectedBookForTagging;

        public ObservableCollection<TagSelectionItem> BookTagPickerTags { get; } = new();

        public bool IsBookTagPickerVisible
        {
            get => _isBookTagPickerVisible;
            set { if (_isBookTagPickerVisible != value) { _isBookTagPickerVisible = value; OnPropertyChanged(); } }
        }

        public string BookTagPickerSearchText
        {
            get => _bookTagPickerSearchText;
            set
            {
                if (_bookTagPickerSearchText != value)
                {
                    _bookTagPickerSearchText = value ?? string.Empty;
                    OnPropertyChanged();
                    RefreshBookTagPickerTags();
                }
            }
        }

        public Book SelectedBookForTagging
        {
            get => _selectedBookForTagging;
            set
            {
                if (_selectedBookForTagging != value)
                {
                    _selectedBookForTagging = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BookTagPickerTitle));
                }
            }
        }

        public string BookTagPickerTitle => SelectedBookForTagging == null ? "Tags du livre" : $"Tags: {SelectedBookForTagging.Title}";

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

        public TagItem SelectedTagForBookFilter
        {
            get => _selectedTagForBookFilter;
            set
            {
                if (_selectedTagForBookFilter != value)
                {
                    _selectedTagForBookFilter = value;
                    OnPropertyChanged();
                    IsViewingTagBooks = value != null;
                    OnPropertyChanged(nameof(CurrentSectionTitle));
                }
            }
        }

        public bool IsViewingTagBooks
        {
            get => _isViewingTagBooks;
            set
            {
                if (_isViewingTagBooks != value)
                {
                    _isViewingTagBooks = value;
                    OnPropertyChanged();
                    RefreshVisibleBooks();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel(DatabaseService dbService, LocalBooksService localBooksService, BookApiService bookApiService)
        {
            _dbService = dbService;
            _localBooksService = localBooksService;
            _bookApiService = bookApiService;
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
                            book.Title = existing.Title;
                            book.Author = existing.Author;
                            book.InsertionDate = existing.InsertionDate;
                        }

                        await _dbService.SaveBookAsync(book);
                    }
                }

                var items = await _dbService.GetBooksAsync();

                var dbTags = await _dbService.GetAllTagsAsync();
                if (dbTags.Count == 0 && items.Count > 0)
                {
                    var initialTags = new[] { "Classiques", "Aventure", "Voyage", "Lecture", "Favoris" };
                    foreach (var t in initialTags) await _dbService.GetOrCreateTagAsync(t);
                    dbTags = await _dbService.GetAllTagsAsync();

                    foreach (var book in items)
                    {
                        var tagsForBook = new List<string> { "Classiques" };
                        if (book.Author.Contains("Verne") || book.Author.Contains("Dumas") || book.Author.Contains("Doyle")) tagsForBook.Add("Aventure");
                        if (book.Title.Contains("tour du monde") || book.Title.Contains("Voyage")) tagsForBook.Add("Voyage");
                        if (book.Title.Contains("Twist") || book.Title.Contains("Carol")) tagsForBook.Add("Lecture");
                        await _dbService.SetTagsForBookAsync(book.Id, tagsForBook);
                    }
                    if (items.Count >= 2)
                    {
                        var firstTwo = items.Take(2).ToList();
                        foreach (var b in firstTwo)
                        {
                            var tags = await _dbService.GetTagsForBookAsync(b.Id);
                            var tNames = tags.Select(t => t.Name).ToList();
                            tNames.Add("Favoris");
                            await _dbService.SetTagsForBookAsync(b.Id, tNames);
                        }
                    }
                }

                _tagAssignments.Clear();
                foreach (var tag in dbTags)
                {
                    var bookIds = await _dbService.GetBookIdsForTagAsync(tag.Name);
                    _tagAssignments[tag.Name] = new HashSet<int>(bookIds);
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _allBooks.Clear();
                    _allBooks.AddRange(items);

                    _allTags.Clear();
                    foreach (var tag in dbTags)
                    {
                        _allTags.Add(new TagItem(tag.Name));
                    }

                    ApplyTagCounts();
                    RefreshTagFilterOptions();
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

        public async Task FetchBooksFromApiAsync()
        {
            try
            {
                IsLoading = true;
                CloseAddBook();
                var apiBooks = await _bookApiService.FetchBooksFromApiAsync();
                
                if (apiBooks != null && apiBooks.Any())
                {
                    foreach (var book in apiBooks)
                    {
                        book.InsertionDate = DateTime.Now;
                        if (string.IsNullOrEmpty(book.CoverImage))
                        {
                            book.CoverImage = "book_icon.png";
                        }
                        
                        await _dbService.SaveBookAsync(book);
                        
                        _allBooks.Insert(0, book);
                    }

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        RefreshVisibleCollections();
                    });
                }
            }
            finally
            {
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

        public void ViewBooksForTag(TagItem tag)
        {
            if (tag == null)
                return;

            SelectedTagForBookFilter = tag;
            IsBooksView = true;
        }

        public void ClearTagFilter()
        {
            SelectedTagForBookFilter = null;
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
            NewBookTitle = string.Empty;
            NewBookAuthor = string.Empty;
            NewBookEpubPath = string.Empty;
            NewBookTag = string.Empty;
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
                        
                        await _dbService.SetTagsForBookAsync(newBook.Id, new List<string> { existingTag.Name });
                    }

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ApplyTagCounts();
                        RefreshTagFilterOptions();
                        RefreshVisibleCollections();
                    });

                    // Reset input fields after successful addition
                    NewBookTitle = string.Empty;
                    NewBookAuthor = string.Empty;
                    NewBookEpubPath = string.Empty;
                    NewBookTag = string.Empty;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
        // --- Add Book Methods End ---

        // --- Create Tag Methods ---
        public void OpenCreateTag()
        {
            NewTagName = string.Empty;
            IsCreateTagVisible = true;
        }

        public void CloseCreateTag()
        {
            IsCreateTagVisible = false;
            NewTagName = string.Empty;
        }

        public async Task ConfirmCreateTagAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTagName)) return;
            
            var tagToFind = NewTagName.Trim();
            var existingTag = _allTags.FirstOrDefault(t => t.Name.Equals(tagToFind, StringComparison.OrdinalIgnoreCase));
            if (existingTag == null)
            {
                await _dbService.GetOrCreateTagAsync(tagToFind);
                var newTagItem = new TagItem(tagToFind);
                _allTags.Add(newTagItem);
                _tagAssignments[tagToFind] = new HashSet<int>();
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    RefreshTagFilterOptions();
                    RefreshVisibleCollections();
                });
            }
            CloseCreateTag();
        }

        public async Task DeleteTagAsync(TagItem tag)
        {
            if (tag == null) return;
            
            await _dbService.DeleteTagAsync(tag.Name);
            
            _tagAssignments.Remove(tag.Name);
            var itemToRemove = _allTags.FirstOrDefault(t => t.Name == tag.Name);
            if (itemToRemove != null)
            {
                _allTags.Remove(itemToRemove);
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                RefreshTagFilterOptions();
                RefreshVisibleCollections();
            });
        }

        // --- Book Tag Picker Methods ---
        private readonly List<TagSelectionItem> _bookTagPickerSourceTags = new();

        public void OpenBookTagPicker(Book book)
        {
            if (book == null) return;
            
            SelectedBookForTagging = book;
            BookTagPickerSearchText = string.Empty;
            _bookTagPickerSourceTags.Clear();
            BookTagPickerTags.Clear();

            foreach (var tag in _allTags)
            {
                bool isAssociated = GetTagAssignments(tag.Name).Contains(book.Id);
                var item = new TagSelectionItem(tag)
                {
                    IsSelected = isAssociated
                };

                _bookTagPickerSourceTags.Add(item);
                BookTagPickerTags.Add(item);
            }

            IsBookTagPickerVisible = true;
        }

        public void CloseBookTagPicker()
        {
            IsBookTagPickerVisible = false;
            SelectedBookForTagging = null;
            BookTagPickerTags.Clear();
        }

        public void ConfirmBookTagPickerSelection()
        {
            if (SelectedBookForTagging == null) return;

            var selectedTagNames = _bookTagPickerSourceTags.Where(item => item.IsSelected).Select(item => item.Tag.Name).ToList();
            var bookId = SelectedBookForTagging.Id;

            // Update in memory
            foreach (var tag in _allTags)
            {
                var tagSet = GetTagAssignments(tag.Name);
                if (selectedTagNames.Contains(tag.Name))
                {
                    tagSet.Add(bookId);
                }
                else
                {
                    tagSet.Remove(bookId);
                }
            }

            // Sync to DB
            Task.Run(async () =>
            {
                await _dbService.SetTagsForBookAsync(bookId, selectedTagNames);
            });

            ApplyTagCounts();
            CloseBookTagPicker();
            RefreshVisibleCollections();
        }
        
        private void RefreshBookTagPickerTags()
        {
            if (!IsBookTagPickerVisible) return;

            IEnumerable<TagSelectionItem> query = _bookTagPickerSourceTags;

            if (!string.IsNullOrWhiteSpace(BookTagPickerSearchText))
            {
                query = query.Where(item => item.Tag.Name.Contains(BookTagPickerSearchText, StringComparison.OrdinalIgnoreCase));
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                BookTagPickerTags.Clear();
                foreach (var item in query) BookTagPickerTags.Add(item);
            });
        }

        public void ConfirmTagPickerSelection()
        {
            if (SelectedTag == null)
                return;

            var selectedIds = _tagPickerSourceBooks.Where(item => item.IsSelected).Select(item => item.Book.Id).ToHashSet();
            _tagAssignments[SelectedTag.Name] = selectedIds;
            
            Task.Run(async () =>
            {
                foreach (var book in _allBooks)
                {
                    bool isSelected = selectedIds.Contains(book.Id);
                    var currentTags = await _dbService.GetTagsForBookAsync(book.Id);
                    var currentTagNames = currentTags.Select(t => t.Name).ToList();
                    
                    if (isSelected && !currentTagNames.Contains(SelectedTag.Name))
                    {
                        currentTagNames.Add(SelectedTag.Name);
                        await _dbService.SetTagsForBookAsync(book.Id, currentTagNames);
                    }
                    else if (!isSelected && currentTagNames.Contains(SelectedTag.Name))
                    {
                        currentTagNames.Remove(SelectedTag.Name);
                        await _dbService.SetTagsForBookAsync(book.Id, currentTagNames);
                    }
                }
            });

            ApplyTagCounts();
            CloseTagPicker();
            RefreshVisibleCollections();
        }

        // EnsureDefaultTags and SeedTagAssignments were removed

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
            RefreshBookTagPickerTags();
        }

        private void RefreshVisibleBooks()
        {
            IEnumerable<Book> query = _allBooks;

            // Filter by selected tag if viewing tag books
            if (IsViewingTagBooks && SelectedTagForBookFilter != null)
            {
                var tagBookIds = GetTagAssignments(SelectedTagForBookFilter.Name);
                query = query.Where(book => tagBookIds.Contains(book.Id));
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(book =>
                    (book.Title?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (book.Author?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (book.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(SelectedTagFilter) && SelectedTagFilter != "Tous les tags" && !IsViewingTagBooks)
            {
                var ids = GetTagAssignments(SelectedTagFilter);
                query = query.Where(book => ids.Contains(book.Id));
            }

            query = SelectedSortOption switch
            {
                "Titre" => query.OrderBy(book => book.Title),
                "Auteur" => query.OrderBy(book => book.Author),
                _ => query.OrderByDescending(book => book.InsertionDate)
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

        public async Task DeleteBookAsync(Book book)
        {
            if (book == null)
                return;

            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Deleting book: {book.Title}");

                // Delete from local file system
                await _localBooksService.DeleteBookAsync(book);

                // Delete from database
                await _dbService.DeleteBookAsync(book.Id);

                // Remove from all collections
                _allBooks.Remove(book);

                // Remove from tag assignments
                foreach (var tagName in _tagAssignments.Keys)
                {
                    _tagAssignments[tagName].Remove(book.Id);
                }

                // Refresh UI
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ApplyTagCounts();
                    RefreshTagFilterOptions();
                    RefreshVisibleCollections();
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] Book deleted successfully: {book.Title}");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Error deleting book: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
