using Microsoft.Playwright;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace TikTok_Downloader
{
    internal enum MediaKind
    {
        Video,
        Photo
    }

    internal sealed class KeywordDownloader : IDisposable
    {
        private const int DefaultDownloadCount = 10;
        private const int MaximumDownloadCount = 500;
        private const string TikWmApiUrl = "https://www.tikwm.com/api/";

        private readonly HttpClient httpClient;
        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public KeywordDownloader()
        {
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(3)
            };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0 Safari/537.36");
        }

        public static void PrintHelp()
        {
            Console.WriteLine("TikTok Keyword Downloader");
            Console.WriteLine();
            Console.WriteLine("Jalankan TikTok Downloader.bat, lalu isi keyword, tipe media, dan jumlah post.");
            Console.WriteLine("Hasil disimpan di folder downloads di samping file BAT.");
            Console.WriteLine("Tekan Ctrl+C kapan saja untuk membatalkan.");
        }

        public async Task<int> RunInteractiveAsync(CancellationToken cancellationToken)
        {
            PrintHeader();

            string keyword = PromptKeyword();
            MediaKind mediaKind = PromptMediaKind();
            int requestedCount = PromptDownloadCount();

            string kindFolder = mediaKind == MediaKind.Video ? "videos" : "photos";
            string outputDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                "downloads",
                SanitizeFileName(keyword, "keyword", 60),
                kindFolder);
            Directory.CreateDirectory(outputDirectory);

            Console.WriteLine();
            Console.WriteLine($"Mencari {requestedCount} post {MediaLabel(mediaKind)} untuk \"{keyword}\"...");
            Console.WriteLine("Browser akan terbuka. Jika muncul CAPTCHA atau login, selesaikan di browser.");

            int candidateTarget = Math.Min(MaximumDownloadCount, requestedCount + Math.Min(requestedCount, 20));
            IReadOnlyList<string> postUrls = await SearchPostUrlsAsync(
                keyword,
                mediaKind,
                candidateTarget,
                cancellationToken);

            if (postUrls.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Tidak ada post yang ditemukan. Coba keyword lain atau selesaikan CAPTCHA TikTok.");
                Console.ResetColor();
                return 2;
            }

            Console.WriteLine($"Ditemukan {postUrls.Count} kandidat. Mulai download...");
            Console.WriteLine();

            int downloaded = 0;
            int failed = 0;

            foreach (string postUrl in postUrls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (downloaded >= requestedCount)
                {
                    break;
                }

                int sequence = downloaded + failed + 1;
                Console.Write($"[{sequence}/{postUrls.Count}] Mengambil data post... ");

                try
                {
                    TikWmPost? post = await GetPostAsync(postUrl, cancellationToken);
                    if (post is null || !MatchesMediaKind(post, mediaKind))
                    {
                        failed++;
                        WriteStatus("dilewati, tipe media tidak cocok.", ConsoleColor.Yellow);
                        continue;
                    }

                    int filesWritten = mediaKind == MediaKind.Video
                        ? await DownloadVideoAsync(post, outputDirectory, cancellationToken)
                        : await DownloadPhotosAsync(post, outputDirectory, cancellationToken);

                    if (filesWritten == 0)
                    {
                        failed++;
                        WriteStatus("gagal, URL media kosong.", ConsoleColor.Red);
                        continue;
                    }

                    downloaded++;
                    WriteStatus($"selesai ({filesWritten} file).", ConsoleColor.Green);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    failed++;
                    WriteStatus($"gagal: {exception.Message}", ConsoleColor.Red);
                }

                await Task.Delay(900, cancellationToken);
            }

            Console.WriteLine();
            Console.WriteLine($"Selesai: {downloaded} post berhasil, {failed} kandidat gagal atau dilewati.");
            Console.WriteLine($"Folder hasil: {outputDirectory}");

            if (downloaded < requestedCount)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Target {requestedCount} post belum tercapai karena hasil pencarian atau API terbatas.");
                Console.ResetColor();
                return 3;
            }

            return 0;
        }

        private static void PrintHeader()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("TikTok Keyword Downloader");
            Console.ResetColor();
            Console.WriteLine("Cari dan simpan post TikTok berdasarkan keyword.");
            Console.WriteLine();
        }

        private static string PromptKeyword()
        {
            while (true)
            {
                Console.Write("Keyword pencarian: ");
                string? value = Console.ReadLine()?.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }

                WriteStatus("Keyword tidak boleh kosong.", ConsoleColor.Yellow);
            }
        }

        private static MediaKind PromptMediaKind()
        {
            while (true)
            {
                Console.Write("Pilih media [1] Video  [2] Foto: ");
                string? value = Console.ReadLine()?.Trim();

                if (value == "1" || value?.Equals("video", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return MediaKind.Video;
                }

                if (value == "2" || value?.Equals("foto", StringComparison.OrdinalIgnoreCase) == true ||
                                    value?.Equals("photo", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return MediaKind.Photo;
                }

                WriteStatus("Masukkan 1 untuk Video atau 2 untuk Foto.", ConsoleColor.Yellow);
            }
        }

        private static int PromptDownloadCount()
        {
            while (true)
            {
                Console.Write($"Jumlah post [{DefaultDownloadCount}]: ");
                string? value = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(value))
                {
                    return DefaultDownloadCount;
                }

                if (int.TryParse(value, out int count) && count is > 0 and <= MaximumDownloadCount)
                {
                    return count;
                }

                WriteStatus($"Masukkan angka 1 sampai {MaximumDownloadCount}.", ConsoleColor.Yellow);
            }
        }

        private static async Task<IReadOnlyList<string>> SearchPostUrlsAsync(
            string keyword,
            MediaKind mediaKind,
            int targetCount,
            CancellationToken cancellationToken)
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            string userDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TikTokKeywordDownloader",
                "browser-profile");
            Directory.CreateDirectory(userDataDirectory);

            string? browserExecutable = FindInstalledBrowser();
            var launchOptions = new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                ExecutablePath = browserExecutable,
                Args = new[] { "--start-maximized" },
                ViewportSize = ViewportSize.NoViewport
            };

            IBrowserContext context;
            try
            {
                context = await playwright.Chromium.LaunchPersistentContextAsync(userDataDirectory, launchOptions);
            }
            catch (PlaywrightException exception)
            {
                string browserHint = browserExecutable is null
                    ? "Install Google Chrome atau Microsoft Edge, lalu coba lagi."
                    : "Tutup browser TikTok yang masih memakai profil downloader, lalu coba lagi.";
                throw new InvalidOperationException($"Browser tidak dapat dibuka. {browserHint}", exception);
            }

            try
            {
                IPage page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
                page.SetDefaultTimeout(60_000);

                string category = mediaKind == MediaKind.Video ? "video" : "photo";
                string searchUrl = $"https://www.tiktok.com/search/{category}?q={Uri.EscapeDataString(keyword)}";
                await page.GotoAsync(searchUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 120_000
                });

                string linkFragment = mediaKind == MediaKind.Video ? "/video/" : "/photo/";
                string selector = $"a[href*='{linkFragment}']";
                var collected = new List<string>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int unchangedRounds = 0;

                for (int round = 0; round < 80 && collected.Count < targetCount; round++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(1_250, cancellationToken);

                    string[] links = await page.EvaluateAsync<string[]>(
                        "selector => Array.from(document.querySelectorAll(selector), element => element.href.split('?')[0])",
                        selector) ?? Array.Empty<string>();

                    int countBefore = collected.Count;
                    foreach (string link in links)
                    {
                        if (link.Contains(linkFragment, StringComparison.OrdinalIgnoreCase) && seen.Add(link))
                        {
                            collected.Add(link);
                        }
                    }

                    if (collected.Count >= targetCount)
                    {
                        break;
                    }

                    unchangedRounds = collected.Count == countBefore ? unchangedRounds + 1 : 0;

                    if (round == 7 && collected.Count == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Belum ada hasil. Selesaikan CAPTCHA/login di browser jika diminta; pencarian tetap berjalan.");
                        Console.ResetColor();
                    }

                    if (unchangedRounds >= 10 && collected.Count > 0)
                    {
                        break;
                    }

                    await page.EvaluateAsync("window.scrollBy(0, Math.max(window.innerHeight * 2, 1400))");
                }

                return collected.Take(targetCount).ToArray();
            }
            finally
            {
                await context.CloseAsync();
            }
        }

        private async Task<TikWmPost?> GetPostAsync(string postUrl, CancellationToken cancellationToken)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using var form = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["url"] = postUrl,
                        ["hd"] = "1"
                    });
                    using HttpResponseMessage response = await httpClient.PostAsync(TikWmApiUrl, form, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    TikWmResponse? payload = await JsonSerializer.DeserializeAsync<TikWmResponse>(
                        responseStream,
                        jsonOptions,
                        cancellationToken);

                    if (payload?.Code == 0 && payload.Data is not null)
                    {
                        return payload.Data;
                    }

                    lastException = new InvalidOperationException(payload?.Message ?? "TikWM tidak mengembalikan data post.");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    lastException = exception;
                }

                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
                }
            }

            throw new InvalidOperationException(lastException?.Message ?? "Data post tidak dapat diambil.");
        }

        private async Task<int> DownloadVideoAsync(
            TikWmPost post,
            string outputDirectory,
            CancellationToken cancellationToken)
        {
            string? mediaUrl = FirstNotEmpty(post.HdPlay, post.Play, post.WatermarkPlay);
            if (mediaUrl is null)
            {
                return 0;
            }

            string baseName = BuildCaptionFileName(post.Title);
            string destinationPath = GetAvailablePath(outputDirectory, baseName, ".mp4");
            await DownloadFileAsync(mediaUrl, destinationPath, cancellationToken);
            return 1;
        }

        private async Task<int> DownloadPhotosAsync(
            TikWmPost post,
            string outputDirectory,
            CancellationToken cancellationToken)
        {
            if (post.Images is null || post.Images.Count == 0)
            {
                return 0;
            }

            string baseName = BuildCaptionFileName(post.Title);
            int written = 0;

            for (int index = 0; index < post.Images.Count; index++)
            {
                string imageUrl = post.Images[index];
                using HttpResponseMessage response = await httpClient.GetAsync(
                    imageUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                response.EnsureSuccessStatusCode();

                string extension = GetImageExtension(response.Content.Headers.ContentType);
                string numberedName = $"{baseName}_{index + 1:00}";
                string destinationPath = GetAvailablePath(outputDirectory, numberedName, extension);

                await using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var output = new FileStream(
                    destinationPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81_920,
                    useAsync: true);
                await input.CopyToAsync(output, cancellationToken);
                written++;
            }

            return written;
        }

        private async Task DownloadFileAsync(
            string mediaUrl,
            string destinationPath,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await httpClient.GetAsync(
                mediaUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = new FileStream(
                destinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81_920,
                useAsync: true);
            await input.CopyToAsync(output, cancellationToken);
        }

        private static bool MatchesMediaKind(TikWmPost post, MediaKind mediaKind)
        {
            bool isPhoto = post.Images is { Count: > 0 };
            return mediaKind == MediaKind.Photo
                ? isPhoto
                : !isPhoto && FirstNotEmpty(post.HdPlay, post.Play, post.WatermarkPlay) is not null;
        }

        private static string BuildCaptionFileName(string? caption)
        {
            return SanitizeFileName(caption, "tiktok", 140);
        }

        private static string SanitizeFileName(string? value, string fallback, int maximumLength)
        {
            string candidate = string.IsNullOrWhiteSpace(value) ? fallback : value.Normalize();
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            candidate = new string(candidate.Select(character =>
                invalidCharacters.Contains(character) || char.IsControl(character) ? ' ' : character).ToArray());
            candidate = Regex.Replace(candidate, @"\s+", " ").Trim(' ', '.');

            if (candidate.Length > maximumLength)
            {
                candidate = candidate[..maximumLength].Trim(' ', '.');
            }

            if (string.IsNullOrWhiteSpace(candidate) || IsReservedWindowsName(candidate))
            {
                candidate = fallback;
            }

            return candidate;
        }

        private static bool IsReservedWindowsName(string value)
        {
            string name = value.Split('.')[0];
            return Regex.IsMatch(name, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", RegexOptions.IgnoreCase);
        }

        private static string GetAvailablePath(string directory, string baseName, string extension)
        {
            string candidate = Path.Combine(directory, baseName + extension);
            if (!File.Exists(candidate))
            {
                return candidate;
            }

            for (int copy = 2; copy < 10_000; copy++)
            {
                candidate = Path.Combine(directory, $"{baseName}_{copy}{extension}");
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new IOException($"Tidak bisa menentukan nama file unik untuk {baseName}{extension}.");
        }

        private static string GetImageExtension(MediaTypeHeaderValue? contentType)
        {
            return contentType?.MediaType?.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".jpg"
            };
        }

        private static string? FirstNotEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }

        private static string MediaLabel(MediaKind mediaKind)
        {
            return mediaKind == MediaKind.Video ? "video" : "foto";
        }

        private static void WriteStatus(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static string? FindInstalledBrowser()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            string[] candidates =
            {
                Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(localAppData, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(programFiles, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
                Path.Combine(localAppData, "BraveSoftware", "Brave-Browser", "Application", "brave.exe")
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        private sealed class TikWmResponse
        {
            [JsonPropertyName("code")]
            public int Code { get; init; }

            [JsonPropertyName("msg")]
            public string? Message { get; init; }

            [JsonPropertyName("data")]
            public TikWmPost? Data { get; init; }
        }

        private sealed class TikWmPost
        {
            [JsonPropertyName("id")]
            public string? Id { get; init; }

            [JsonPropertyName("title")]
            public string? Title { get; init; }

            [JsonPropertyName("play")]
            public string? Play { get; init; }

            [JsonPropertyName("wmplay")]
            public string? WatermarkPlay { get; init; }

            [JsonPropertyName("hdplay")]
            public string? HdPlay { get; init; }

            [JsonPropertyName("images")]
            public List<string>? Images { get; init; }
        }
    }
}
