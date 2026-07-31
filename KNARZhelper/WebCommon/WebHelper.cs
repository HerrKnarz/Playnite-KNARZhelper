using KNARZhelper.FilesCommon;
using KNARZhelper.WebCommon.Models;
using Playnite.SDK;
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace KNARZhelper.WebCommon
{
    public static class WebHelper
    {
        public static readonly string AgentString =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";

        public static void CatchError(UrlLoadResult urlLoadResult, Exception exception, string url)
        {
            if (exception is WebException webEx)
            {
                if (webEx.Response != null)
                {
                    var response = webEx.Response;
                    var dataStream = response.GetResponseStream();

                    if (dataStream != null)
                    {
                        var reader = new StreamReader(dataStream);
                        urlLoadResult.ErrorDetails = reader.ReadToEnd();
                        urlLoadResult.StatusCode = ((HttpWebResponse)response).StatusCode;
                        reader.Close();

                        Log.Debug($"Error loading url {url} => status code {urlLoadResult.StatusCode}");
                    }
                }
            }
            else if (exception is Exception ex)
            {
                urlLoadResult.ErrorDetails = ex.Message;
                urlLoadResult.StatusCode = HttpStatusCode.BadRequest;
                Log.Error(ex, $"Error loading url {url} => {urlLoadResult.ErrorDetails}");
            }
        }

        /// <summary>
        /// Removes the scheme of a URL and adds a missing trailing slash. Is used to compare URLs
        /// with different schemes
        /// </summary>
        /// <param name="url">URL to clean up</param>
        /// <returns>cleaned up URL</returns>
        public static string CleanUpUrl(string url)
        {
            try
            {
                var uri = new Uri(url);

                var urlWithoutScheme = uri.Host + uri.PathAndQuery + uri.Fragment;

                return !urlWithoutScheme.EndsWith("/") ? urlWithoutScheme + "/" : urlWithoutScheme;
            }
            catch (Exception)
            {
                return url;
            }
        }

        /// <summary>
        /// Gets the content of the title tag from a html page.
        /// </summary>
        /// <param name="htmlSource">html page to parse</param>
        /// <returns>decoded title of the page</returns>
        public static string GetPageTitle(string htmlSource) =>
            WebUtility.HtmlDecode(Regex.Match(htmlSource, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value.Trim());

        /// <summary>
        /// Loads a web page with a smart wait to bypass Cloudflare protection.
        /// </summary>
        /// <param name="webView">WebView instance to use for loading the page</param>
        /// <param name="url">URL of the page to load</param>
        /// <param name="debugMode">
        /// If true, saves the last fetched HTML to a file for debugging purposes
        /// </param>
        /// <param name="pluginId">Plugin ID to determine the path for saving the debug file</param>
        /// <returns>HTML source of the loaded page</returns>
        public static string LoadPageWithSmartWait(IWebView webView, string url, bool debugMode = false, Guid pluginId = default)
        {
            var htmlSource = string.Empty;

            webView.NavigateAndWait(url);

            var maxRetries = 10; // 10 attempts of 1 second
            var attempts = 0;

            while (attempts < maxRetries)
            {
                htmlSource = webView.GetPageSource();

                // Check if we passed the Cloudflare barrier. If the HTML DOES NOT contain the block
                // messages, it means the page loaded!
                if (!string.IsNullOrEmpty(htmlSource) &&
                    !htmlSource.Contains("Just a moment...") &&
                    !htmlSource.Contains("cf-browser-verification") &&
                    !htmlSource.Contains("challenges.cloudflare.com"))
                {
                    htmlSource = webView.GetPageSource();
                    break;
                }

                // If still on the Cloudflare screen, wait 1 second and try reading the HTML again
                Thread.Sleep(1000);
                attempts++;
            }

            if (debugMode)
            {
                var basePath = API.Instance.Addons.Plugins.Find(p => p.Id == pluginId).GetPluginUserDataPath();

                FileHelper.WriteStringToFile(Path.Combine(basePath, "last_fetched.html"), htmlSource);
            }

            return htmlSource;
        }
    }
}
