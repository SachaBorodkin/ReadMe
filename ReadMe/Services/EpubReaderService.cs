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

                var epubContent = new EpubContent();

                using (var zipArchive = ZipFile.OpenRead(epubFilePath))
                {

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
                                .ToDictionary(x => x.Attribute("id")?.Value ?? "", x => x.Attribute("href")?.Value ?? "");

                            int chapterIndex = 0;
                            foreach (var spineItem in spine.Descendants(opfNs + "itemref"))
                            {
                                var itemId = spineItem.Attribute("idref")?.Value;
                                if (itemId != null && manifestItems.TryGetValue(itemId, out var href))
                                {
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
                }

                System.Diagnostics.Debug.WriteLine($"[EpubReaderService] Loaded {epubContent.Chapters.Count} chapters");
                return epubContent;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EpubReaderService] Error loading EPUB: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
    }
}
