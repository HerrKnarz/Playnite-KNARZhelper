using ComposableAsync;
using RateLimiter;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace KNARZhelper.WebCommon
{
    public interface IHttpClient
    {
        string DownloadString(string url, CancellationToken cancellationToken = default);

        Task<string> DownloadStringAsync(string url, CancellationToken cancellationToken = default);
    }

    public class HttpClientWrapper : IHttpClient
    {
        private readonly HttpClient _httpClient;

        public HttpClientWrapper(string userAgent = null, int rateLimit = 0)
        {
            if (rateLimit > 0)
            {
                var handler = TimeLimiter
                          .GetFromMaxCountByInterval(rateLimit, TimeSpan.FromMinutes(1))
                          .AsDelegatingHandler();
                _httpClient = new HttpClient(handler);
            }
            else
            {
                _httpClient = new HttpClient();
            }

            var assembly = Assembly.GetExecutingAssembly().GetName();

            if (string.IsNullOrEmpty(userAgent))
            {
                userAgent = $"Playnite {assembly.Name} Addon/{assembly.Version} (alex@knarzwerk.de)";
            }

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        }

        public string DownloadString(string url, CancellationToken cancellationToken) => AsyncHelper.RunSync(async () => await DownloadStringAsync(url, cancellationToken));

        public async Task<string> DownloadStringAsync(string url, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
    }
}
