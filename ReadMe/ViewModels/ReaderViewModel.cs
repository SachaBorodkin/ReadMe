using System.ComponentModel;
using System.Runtime.CompilerServices;
using ReadMe.Models;
using ReadMe.Services;

namespace ReadMe.ViewModels
{
    public class ReaderViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService;
        private readonly EpubReaderService _epubReaderService;
        private Book _currentBook;
        private EpubReaderService.EpubContent _epubContent;
        private int _currentChapterIndex;
        private string _currentChapterContent;
        private string _currentChapterTitle;

        public Book CurrentBook
        {
            get => _currentBook;
            set
            {
                if (_currentBook != value)
                {
                    _currentBook = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentChapterTitle
        {
            get => _currentChapterTitle;
            set
            {
                if (_currentChapterTitle != value)
                {
                    _currentChapterTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentChapterContent
        {
            get => _currentChapterContent;
            set
            {
                if (_currentChapterContent != value)
                {
                    _currentChapterContent = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CurrentChapterIndex
        {
            get => _currentChapterIndex;
            set
            {
                if (_currentChapterIndex != value)
                {
                    _currentChapterIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalChapters => _epubContent?.Chapters.Count ?? 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public ReaderViewModel(DatabaseService dbService, EpubReaderService epubReaderService)
        {
            _dbService = dbService;
            _epubReaderService = epubReaderService;
        }

        public async Task LoadBookAsync(Book book)
        {
            try
            {
                CurrentBook = book;
                System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] Loading book: {book.Title}");

                var epubFileName = Path.GetFileName(book.EpubFilePath);
                var epubPath = Path.Combine(FileSystem.AppDataDirectory, "books", epubFileName);

                System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] EPUB path: {epubPath}");

                if (!File.Exists(epubPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] EPUB file not found, attempting download...");

                    var booksDir = Path.Combine(FileSystem.AppDataDirectory, "books");
                    if (!Directory.Exists(booksDir))
                        Directory.CreateDirectory(booksDir);

                    using (var client = new HttpClient())
                    {
                        var downloadUrl = $"http://10.0.2.2:3000/files/{epubFileName}";
                        System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] Downloading from: {downloadUrl}");

                        var response = await client.GetAsync(downloadUrl);
                        if (!response.IsSuccessStatusCode)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] Download failed with status: {response.StatusCode}");
                            await Shell.Current.DisplayAlert("Error", $"Failed to download book file (Status: {response.StatusCode})", "OK");
                            return;
                        }

                        var fileContent = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(epubPath, fileContent);
                        System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] Downloaded {fileContent.Length} bytes");
                    }
                }

                _epubContent = await _epubReaderService.LoadEpubAsync(epubPath);

                int desiredIndex = 0;
                if (book != null)
                {
                    desiredIndex = book.LastPageOpened;
                }

                // Clamp to available chapters
                if (_epubContent?.Chapters != null && _epubContent.Chapters.Count > 0)
                {
                    desiredIndex = Math.Clamp(desiredIndex, 0, _epubContent.Chapters.Count - 1);
                }

                CurrentChapterIndex = desiredIndex;
                LoadChapter(CurrentChapterIndex);

                System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] Book loaded successfully with {TotalChapters} chapters");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] Error loading book: {ex.Message}\n{ex.StackTrace}");
                await Shell.Current.DisplayAlert("Error", $"Failed to load book: {ex.Message}", "OK");
            }
        }

        public void LoadChapter(int chapterIndex)
        {
            try
            {
                if (_epubContent == null || chapterIndex < 0 || chapterIndex >= _epubContent.Chapters.Count)
                    return;

                CurrentChapterIndex = chapterIndex;
                var chapter = _epubContent.Chapters[chapterIndex];
                CurrentChapterTitle = chapter.Title;
                CurrentChapterContent = chapter.HtmlContent;
                // Save progress asynchronously when changing chapters
                _ = SaveProgressAsync();
                System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] Loaded chapter {chapterIndex + 1}: {chapter.Title}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] Error loading chapter: {ex.Message}");
            }
        }

        public async Task SaveProgressAsync()
        {
            try
            {
                if (CurrentBook == null)
                    return;

                CurrentBook.LastPageOpened = CurrentChapterIndex;
                CurrentBook.LastOpenedDate = DateTime.Now;

                await _dbService.SaveBookAsync(CurrentBook);
                System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] Progress saved: Chapter {CurrentChapterIndex + 1}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReaderViewModel] Error saving progress: {ex.Message}");
            }
        }

        public void NextChapter()
        {
            if (CurrentChapterIndex < TotalChapters - 1)
            {
                LoadChapter(CurrentChapterIndex + 1);
            }
        }

        public void PreviousChapter()
        {
            if (CurrentChapterIndex > 0)
            {
                LoadChapter(CurrentChapterIndex - 1);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
