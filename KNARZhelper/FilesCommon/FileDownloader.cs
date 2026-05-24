using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace KNARZhelper.FilesCommon
{
    /// <summary>
    /// A utility class for downloading files asynchronously using HttpClient.
    /// </summary>
    public class FileDownloader : IDisposable
    {
        private static FileDownloader _instance;
        private bool _disposed;
        private HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileDownloader"/> class.
        /// </summary>
        /// <param name="httpClient">An optional HttpClient instance to use for downloading files.</param>
        public FileDownloader(HttpClient httpClient = null)
        {
            InitHttpClient(httpClient);
        }

        /// <summary>
        /// Gets the singleton instance of the <see cref="FileDownloader"/> class.
        /// </summary>
        /// <returns>The singleton instance of the <see cref="FileDownloader"/> class.</returns>
        public static FileDownloader Instance() => _instance ?? (_instance = new FileDownloader());

        /// <summary>
        /// Disposes the HttpClient instance used by the FileDownloader.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _httpClient.Dispose();
            GC.SuppressFinalize(this);
            _disposed = true;
        }

        /// <summary>
        /// Downloads a file asynchronously from the <paramref name="uri"/> and places it in the
        /// specified <paramref name="directoryPath"/> with the specified <paramref name="fileName"/>.
        /// </summary>
        /// <param name="directoryPath">
        /// The relative or absolute path to the directory including the filename to be used.
        /// </param>
        /// <param name="uri">The URI for the file to download.</param>
        /// <returns>FileInfo of the downloaded file</returns>
        public async Task<FileInfo> DownloadFileAsync(string directoryPath, Uri uri)
        {
            File.WriteAllBytes(directoryPath, await DownloadFileAsync(uri));

            return new FileInfo(directoryPath);
        }

        /// <summary>
        /// Downloads a file asynchronously from the <paramref name="uri"/> and returns it as a byte array.
        /// </summary>
        /// <param name="uri">The URI for the file to download.</param>
        /// <returns>Byte array of the downloaded file</returns>
        public async Task<byte[]> DownloadFileAsync(Uri uri)
        {
            if (_disposed)
            {
                InitHttpClient();
            }

            return await _httpClient.GetByteArrayAsync(uri);
        }

        private void InitHttpClient(HttpClient httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();

            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
        }
    }
}
