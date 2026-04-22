using System.Net.Http.Json;
using System.Text.Json;
using ReadMe.Models;

namespace ReadMe.Services
{
    public class BookApiService
    {
        private readonly HttpClient _httpClient;
        
        private readonly string ApiUrl = GetApiUrl();

        private static string GetApiUrl()
        {
#if DEBUG
            if (DeviceInfo.Platform == DevicePlatform.Android)
                return "http://10.0.2.2:3000/books";
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
                return "http://localhost:3000/books";
            else
                return "http://127.0.0.1:3000/books";
#else
            return "http://127.0.0.1:3000/books";
#endif
        }

        public BookApiService()
        {
            var handler = new HttpClientHandler();
#if DEBUG
            // Allow cleartext traffic for development
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<List<Book>> FetchBooksFromApiAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[BookApiService] API URL: {ApiUrl}");
                System.Diagnostics.Debug.WriteLine($"[BookApiService] Platform: {DeviceInfo.Platform}");
                
                var response = await _httpClient.GetAsync(ApiUrl);
                System.Diagnostics.Debug.WriteLine($"[BookApiService] Response Status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[BookApiService] API Error: {response.StatusCode} - {response.ReasonPhrase}");
                    return new List<Book>();
                }

                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[BookApiService] Response Content Length: {content.Length}");
                System.Diagnostics.Debug.WriteLine($"[BookApiService] Response Preview: {content.Substring(0, Math.Min(200, content.Length))}");

                var books = System.Text.Json.JsonSerializer.Deserialize<List<Book>>(
                    content, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                
                System.Diagnostics.Debug.WriteLine($"[BookApiService] Successfully deserialized {books?.Count ?? 0} books");
                return books ?? new List<Book>();
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"[BookApiService] HTTP Error: {httpEx.Message}\n{httpEx.StackTrace}");
                return new List<Book>();
            }
            catch (JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine($"[BookApiService] JSON parsing error: {jsonEx.Message}\n{jsonEx.StackTrace}");
                return new List<Book>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BookApiService] Unexpected error: {ex.Message}\n{ex.StackTrace}");
                return new List<Book>();
            }
        }
    }
}