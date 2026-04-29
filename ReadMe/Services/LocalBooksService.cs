using ReadMe.Models;
using System.IO.Compression;
using System.Xml.Linq;

namespace ReadMe.Services
{
    public class LocalBooksService
    {
        private readonly string _booksDirectory;
        private List<Book> _cachedBooks = new();

        public LocalBooksService()
        {

            _booksDirectory = Path.Combine(FileSystem.AppDataDirectory, "books");
            System.Diagnostics.Debug.WriteLine($"[LocalBooksService] Books directory: {_booksDirectory}");
        }

        public async Task<List<Book>> LoadLocalBooksAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[LocalBooksService] === Starting LoadLocalBooksAsync ===");

                if (!Directory.Exists(_booksDirectory))
                {
                    Directory.CreateDirectory(_booksDirectory);
                    System.Diagnostics.Debug.WriteLine($"[LocalBooksService] Created books directory: {_booksDirectory}");
                }

                await EnsureBooksAreCopiedAsync();

                var books = new List<Book>();
                var epubFiles = Directory.GetFiles(_booksDirectory, "*.epub");
                System.Diagnostics.Debug.WriteLine($"[LocalBooksService] Found {epubFiles.Length} EPUB files");

                int id = 1;
                foreach (var epubFile in epubFiles)
                {
                    try
                    {
                        var book = await CreateBookFromEpubAsync(epubFile, id);
                        if (book != null)
                        {
                            books.Add(book);
                            System.Diagnostics.Debug.WriteLine($"[LocalBooksService] Loaded book: {book.Title} by {book.Author}");
                        }
                        id++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LocalBooksService] Error processing file {epubFile}: {ex.Message}");
                    }
                }

                _cachedBooks = books;
                System.Diagnostics.Debug.WriteLine($"[LocalBooksService] Loaded {books.Count} books successfully");
                return books;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalBooksService] Error in LoadLocalBooksAsync: {ex.Message}\n{ex.StackTrace}");
                return new List<Book>();
            }
        }

        private async Task EnsureBooksAreCopiedAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[LocalBooksService] === Ensuring books are copied ===");

                var bookNames = new[]
                {
                    "Dickens, Charles - A Christmas Carol.epub",
                    "Dickens, Charles - Oliver Twist.epub",
                    "Doyle, Artur Conan - Sherlock Holmes.epub",
                    "Dumas, Alexandre - Les trois mousquetaires.epub",
                    "La Fontaine, Jean de - Fables.epub",
                    "Verne, Jules - Le tour du monde en quatre-vingts jours.epub"
                };

                foreach (var bookName in bookNames)
                {
                    var targetPath = Path.Combine(_booksDirectory, bookName);

                    if (File.Exists(targetPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"[LocalBooksService] Book already exists: {bookName}");
                        continue;
                    }

                    try
                    {
                        var bundledFilePath = $"books/{bookName}";
                        System.Diagnostics.Debug.WriteLine($"[LocalBooksService] Attempting to copy bundled book: {bundledFilePath}");

                        using (var stream = await FileSystem.OpenAppPackageFileAsync(bundledFilePath))
                        {
                            if (stream != null)
                            {
                                using (var fileStream = File.Create(targetPath))
                                {
                                    await stream.CopyToAsync(fileStream);
                                    System.Diagnostics.Debug.WriteLine($"[LocalBooksService] Successfully copied: {bookName}");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[LocalBooksService] Failed to open bundled file: {bundledFilePath}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LocalBooksService] Error copying book {bookName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalBooksService] Error in EnsureBooksAreCopiedAsync: {ex.Message}");
            }
        }

        private async Task<Book> CreateBookFromEpubAsync(string epubFilePath, int id)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(epubFilePath);
                var (author, title) = ParseFilename(fileName);

                var book = new Book
                {
                    Id = id,
                    Title = title,
                    Author = author,
                    EpubFilePath = epubFilePath,
                    TotalPages = await GetEpubPageCountAsync(epubFilePath),
                    CoverImage = "book_icon.png", 
                    Description = $"A book by {author}",
                    Language = "en",
                    UploadedAt = DateTime.Now.ToString("yyyy-MM-dd"),
                    LastPageOpened = 0,
                    LastOpenedDate = DateTime.MinValue
                };

                return book;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalBooksService] Error creating book from {epubFilePath}: {ex.Message}");
                return null;
            }
        }

        private (string Author, string Title) ParseFilename(string filename)
        {

            var parts = filename.Split(new[] { " - " }, StringSplitOptions.None);

            if (parts.Length >= 2)
            {
                var author = parts[0].Trim();
                var title = string.Join(" - ", parts.Skip(1)).Trim();
                return (author, title);
            }
            else if (parts.Length == 1)
            {
                return ("Unknown", parts[0].Trim());
            }

            return ("Unknown", "Unknown");
        }

        private async Task<int> GetEpubPageCountAsync(string epubFilePath)
        {
            try
            {
                using (var zipArchive = ZipFile.OpenRead(epubFilePath))
                {

                    var entries = zipArchive.Entries.Where(e => e.FullName.EndsWith(".xhtml") || e.FullName.EndsWith(".html")).ToList();
                    return Math.Max(entries.Count, 1);
                }
            }
            catch
            {
                return 1;
            }
        }

        public Book GetRandomBook()
        {
            if (_cachedBooks == null || _cachedBooks.Count == 0)
                return null;

            var random = new Random();
            return _cachedBooks[random.Next(_cachedBooks.Count)];
        }

        public List<Book> GetAllBooks()
        {
            return _cachedBooks ?? new List<Book>();
        }
    }
}
