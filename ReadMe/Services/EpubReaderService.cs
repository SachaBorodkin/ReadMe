using System.IO.Compression;
using System.Xml.Linq;

namespace ReadMe.Services
{
    public class EpubReaderService
    {
        public class EpubContent
        {
            public string Title { get; set; }
            public string Author { get; set; }
            public List<EpubChapter> Chapters { get; set; } = new();
            public string CoverImagePath { get; set; }
        }

        public class EpubChapter
        {
            public int Index { get; set; }
            public string Title { get; set; }
            public string HtmlContent { get; set; }
            public string FilePath { get; set; }
        }

        public async Task<EpubContent> LoadEpubAsync(string epubFilePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[EpubReaderService] Loading EPUB from: {epubFilePath}");
                using (var zipArchive = ZipFile.OpenRead(epubFilePath))
                {
                    return await LoadFromArchiveAsync(zipArchive);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EpubReaderService] Error loading EPUB: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task<EpubContent> LoadEpubAsync(byte[] epubBytes)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[EpubReaderService] Loading EPUB from byte array");
                using (var ms = new MemoryStream(epubBytes))
                using (var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read))
                {
                    return await LoadFromArchiveAsync(zipArchive);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EpubReaderService] Error loading EPUB: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private async Task<EpubContent> LoadFromArchiveAsync(ZipArchive zipArchive)
        {
            var epubContent = new EpubContent();

            var containerEntry = zipArchive.GetEntry("META-INF/container.xml");
            if (containerEntry == null)
                throw new Exception("Invalid EPUB: container.xml not found");

            string rootFilePath;
            using (var stream = containerEntry.Open())
            using (var reader = new StreamReader(stream))
            {
                var containerXml = XDocument.Parse(await reader.ReadToEndAsync());
                var ns = XNamespace.Get("urn:oasis:names:tc:opendocument:xmlns:container");
                rootFilePath = containerXml.Descendants(ns + "rootfile")
                    .FirstOrDefault()?.Attribute("full-path")?.Value ?? "content.opf";
            }

            System.Diagnostics.Debug.WriteLine($"[EpubReaderService] Root file: {rootFilePath}");

            var opfEntry = zipArchive.GetEntry(rootFilePath);
            if (opfEntry == null)
                throw new Exception($"Invalid EPUB: {rootFilePath} not found");

            string opfDirectory = Path.GetDirectoryName(rootFilePath) ?? "";

            using (var stream = opfEntry.Open())
            using (var reader = new StreamReader(stream))
            {
                var opfXml = XDocument.Parse(await reader.ReadToEndAsync());
                var opfNs = XNamespace.Get("http://www.idpf.org/2007/opf");

                var metadata = opfXml.Descendants(opfNs + "metadata").FirstOrDefault();
                if (metadata != null)
                {
                    epubContent.Title = metadata.Descendants(XNamespace.Get("http://purl.org/dc/elements/1.1/") + "title").FirstOrDefault()?.Value ?? "Unknown";
                    epubContent.Author = metadata.Descendants(XNamespace.Get("http://purl.org/dc/elements/1.1/") + "creator").FirstOrDefault()?.Value ?? "Unknown";
                }

                var spine = opfXml.Descendants(opfNs + "spine").FirstOrDefault();
                var manifest = opfXml.Descendants(opfNs + "manifest").FirstOrDefault();

                if (spine != null && manifest != null)
                {
                    var manifestItems = manifest.Descendants(opfNs + "item")
                        .ToDictionary(x => x.Attribute("id")?.Value ?? "", x => x);

                    // Extract cover
                    string coverHref = null;
                    var coverItemEpub3 = manifestItems.Values.FirstOrDefault(x => x.Attribute("properties")?.Value.Contains("cover-image") == true);
                    if (coverItemEpub3 != null)
                    {
                        coverHref = coverItemEpub3.Attribute("href")?.Value;
                    }
                    else if (metadata != null)
                    {
                        var metaCover = metadata.Descendants(opfNs + "meta").FirstOrDefault(x => x.Attribute("name")?.Value == "cover");
                        if (metaCover != null)
                        {
                            var coverId = metaCover.Attribute("content")?.Value;
                            if (!string.IsNullOrEmpty(coverId) && manifestItems.TryGetValue(coverId, out var item))
                            {
                                coverHref = item.Attribute("href")?.Value;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(coverHref))
                    {
                        var coverPath = Path.Combine(opfDirectory, coverHref).Replace("\\", "/");
                        var coverEntry = zipArchive.GetEntry(coverPath);
                        if (coverEntry != null)
                        {
                            var tempCoverPath = Path.Combine(FileSystem.AppDataDirectory, $"{Guid.NewGuid()}{Path.GetExtension(coverHref)}");
                            using (var coverStream = coverEntry.Open())
                            using (var fileStream = File.Create(tempCoverPath))
                            {
                                await coverStream.CopyToAsync(fileStream);
                            }
                            epubContent.CoverImagePath = tempCoverPath;
                        }
                    }

                    int chapterIndex = 0;
                    foreach (var spineItem in spine.Descendants(opfNs + "itemref"))
                    {
                        var itemId = spineItem.Attribute("idref")?.Value;
                        if (itemId != null && manifestItems.TryGetValue(itemId, out var itemElement))
                        {
                            var href = itemElement.Attribute("href")?.Value;
                            if (href == null) continue;

                            var filePath = Path.Combine(opfDirectory, href);
                            filePath = filePath.Replace("\\", "/");

                            var entry = zipArchive.GetEntry(filePath);
                            if (entry != null)
                            {
                                using (var stream2 = entry.Open())
                                using (var reader2 = new StreamReader(stream2))
                                {
                                    var htmlContent = await reader2.ReadToEndAsync();

                                    var titleMatch = System.Text.RegularExpressions.Regex.Match(htmlContent, @"<title[^>]*>([^<]+)</title>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                    var chapterTitle = titleMatch.Success ? titleMatch.Groups[1].Value : $"Chapter {chapterIndex + 1}";

                                    epubContent.Chapters.Add(new EpubChapter
                                    {
                                        Index = chapterIndex,
                                        Title = chapterTitle,
                                        HtmlContent = htmlContent,
                                        FilePath = filePath
                                    });

                                    chapterIndex++;
                                }
                            }
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[EpubReaderService] Loaded {epubContent.Chapters.Count} chapters");
            return epubContent;
        }
    }
}
