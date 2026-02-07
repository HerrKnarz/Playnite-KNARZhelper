using HtmlAgilityPack;
using KNARZhelper.WebCommon.Models;
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        /// tries to reach a URL and returns response infos like status code.
        /// </summary>
        /// <param name="url">URL to check</param>
        /// <param name="allowRedirects">If true, a redirect will count as ok.</param>
        /// <param name="checkForContent">Content to check for</param>
        /// <returns>Response infos</returns>
        public static UrlLoadResult CheckUrl(string url, bool allowRedirects = true, string checkForContent = "") =>
            LoadHtmlDocument(url, allowRedirects, false, checkForContent);

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
        /// Loads an HTML document from a URL using the specified method.
        /// </summary>
        /// <param name="url">URL to load</param>
        /// <param name="allowRedirects">If true, redirects are allowed</param>
        /// <param name="needDocument">
        /// If true, the loaded document will be returned in the result. Set to false if you only
        /// want to check for validity and don't need the actual document
        /// </param>
        /// <param name="checkForContent">
        /// Content to check for. Is used to determine if the returned document is valid. For
        /// LoadFromBrowser it also is used to determine if the document is fully loaded
        /// </param>
        /// <returns>Loading result</returns>
        public static UrlLoadResult LoadHtmlDocument(string url, bool allowRedirects = false, bool needDocument = true, string checkForContent = "") => LoadHtmlDocumentAsync(url, allowRedirects, needDocument, checkForContent).Result;

        /// <summary>
        /// Asynchronously loads an HTML document from a URL using the specified method.
        /// </summary>
        /// <param name="url">URL to load</param>
        /// <param name="allowRedirects">If true, redirects are allowed</param>
        /// <param name="needDocument">
        /// If true, the loaded document will be returned in the result. Set to false if you only
        /// want to check for validity and don't need the actual document
        /// </param>
        /// <param name="checkForContent">
        /// Content to check for. Is used to determine if the returned document is valid. For
        /// LoadFromBrowser it also is used to determine if the document is fully loaded
        /// </param>
        /// <returns>Loading result</returns>
        public static async Task<UrlLoadResult> LoadHtmlDocumentAsync(string url, bool allowRedirects = false, bool needDocument = true, string checkForContent = "")
        {
            var result = new UrlLoadResult();

            try
            {
                result = await LoadHtmlDocumentSimpleAsync(url, allowRedirects, checkForContent);

                if (result.Document == null)
                {
                    result.ErrorDetails = $"Error loading HTML document from {url}";
                    result.StatusCode = HttpStatusCode.BadRequest;
                    return result;
                }

                result.PageTitle = result.Document?.DocumentNode?.SelectSingleNode("html/head/title")?.InnerText.Trim();

                if (!needDocument)
                {
                    result.Document = null;
                }
            }
            catch (Exception exception)
            {
                CatchError(result, exception, url);
            }

            return result;
        }

        /// <summary>
        /// Creates a new instance of HtmlWeb with some default settings.
        /// </summary>
        /// <param name="allowRedirects">If true, redirects are allowed</param>
        /// <returns>Configured HtmlWeb instance</returns>
        private static HtmlWeb GetHtmlWeb(bool allowRedirects, TaskCompletionSource<HttpWebResponse> postTask = null)
        {
            var web = new HtmlWeb
            {
                UseCookies = true,
                BrowserTimeout = new TimeSpan(0, 0, 10),
                UserAgent = AgentString
            };

            if (allowRedirects)
            {
                web.PreRequest = delegate (HttpWebRequest request)
                {
                    request.AllowAutoRedirect = allowRedirects;
                    request.KeepAlive = false;
                    request.Timeout = 10 * 1000;
                    return true;
                };
            }

            if (postTask != null)
            {
                web.PostResponse = delegate (HttpWebRequest request, HttpWebResponse response)
                {
                    postTask.SetResult(response);
                };
            }

            return web;
        }

        /// <summary>
        /// Loads the HTML document via the simple Load method of HtmlAgilityPack
        /// </summary>
        /// <param name="url">url to load</param>
        /// <param name="allowRedirects">If true, redirects are allowed</param>
        /// <param name="checkForContent">
        /// Content to check for. If the document doesn't contain this content, it's considered the
        /// wrong page.
        /// </param>
        /// <returns>the url load result</returns>
        private static async Task<UrlLoadResult> LoadHtmlDocumentSimpleAsync(string url, bool allowRedirects = false, string checkForContent = "")
        {
            var result = new UrlLoadResult();

            try
            {
                var tcs = new TaskCompletionSource<HttpWebResponse>();
                var htmlWeb = GetHtmlWeb(allowRedirects, tcs);

                result.Document = htmlWeb.Load(url);

                var httpWebResponse = await tcs.Task;

                result.StatusCode = httpWebResponse.StatusCode == HttpStatusCode.OK
                    ? checkForContent.Length == 0 || result.Document.DocumentNode.InnerHtml.Contains(checkForContent) ? httpWebResponse.StatusCode : HttpStatusCode.NotFound
                    : httpWebResponse.StatusCode;

                result.ResponseUrl = httpWebResponse?.ResponseUri?.AbsoluteUri;
            }
            catch (Exception exception)
            {
                CatchError(result, exception, url);
            }

            return result;
        }
    }
}