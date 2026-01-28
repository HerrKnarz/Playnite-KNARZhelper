using HtmlAgilityPack;
using KNARZhelper.WebCommon.Models;
using Playnite.SDK;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace KNARZhelper.WebCommon
{
    public enum UrlLoadMethod
    {
        /// <summary>
        /// Loads only the header of the URL. This is fast, but many websites return 403 without a
        /// real browser.
        /// </summary>
        Header = 0,

        /// <summary>
        /// Loads the URL via the simple Load method of HtmlAgilityPack
        /// </summary>
        Load = 1,

        /// <summary>
        /// Loads the URL using an offscreen browser instance. This is slower, but can handle more
        /// complex sites. We don't get a StatusCode though, so the validity must be checked in the
        /// document content individually.
        /// </summary>
        OffscreenView = 3 // has to be 3, since we removed an unused one in between.
    }

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
        public static UrlLoadResult CheckUrl(string url, UrlLoadMethod method = UrlLoadMethod.Load, bool allowRedirects = true, string checkForContent = "") =>
            LoadHtmlDocument(url, method, allowRedirects, false, checkForContent);

        /// <summary>
        /// Checks if a URL is reachable by only requesting the header.
        /// </summary>
        /// <param name="url">url to check</param>
        /// <param name="allowRedirects">If true, redirects are allowed</param>
        /// <returns>The response url, status code and an object containing a possible exception</returns>
        public static async Task<UrlLoadResult> CheckUrlSimpleAsync(string url, bool allowRedirects = false)
        {
            var result = new UrlLoadResult();

            try
            {
                var request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "HEAD";
                request.AllowAutoRedirect = allowRedirects;
                request.UserAgent = AgentString;
                request.Timeout = 10000;
                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    result.StatusCode = response.StatusCode;
                    result.ResponseUrl = response.ResponseUri.AbsoluteUri;
                    response.Close();
                }
            }
            catch (Exception exception)
            {
                CatchError(result, exception, url);
            }

            return result;
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
        /// Loads an HTML document from a URL using the specified method.
        /// </summary>
        /// <param name="url">URL to load</param>
        /// <param name="method">Loading method</param>
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
        public static UrlLoadResult LoadHtmlDocument(string url, UrlLoadMethod method = UrlLoadMethod.Load, bool allowRedirects = false, bool needDocument = true, string checkForContent = "") => LoadHtmlDocumentAsync(url, method, allowRedirects, needDocument, checkForContent).Result;

        /// <summary>
        /// Asynchronously loads an HTML document from a URL using the specified method.
        /// </summary>
        /// <param name="url">URL to load</param>
        /// <param name="method">Loading method</param>
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
        public static async Task<UrlLoadResult> LoadHtmlDocumentAsync(string url, UrlLoadMethod method = UrlLoadMethod.Load, bool allowRedirects = false, bool needDocument = true, string checkForContent = "")
        {
            var result = new UrlLoadResult();

            try
            {
                switch (method)
                {
                    case UrlLoadMethod.Header:
                        result = await CheckUrlSimpleAsync(url, allowRedirects);
                        break;

                    case UrlLoadMethod.Load:
                        result = await LoadHtmlDocumentSimpleAsync(url, allowRedirects, checkForContent);
                        break;

                    default:
                        result = LoadHtmlDocumentFromOffscreenView(url, checkForContent);
                        break;
                }

                if (method == UrlLoadMethod.Header)
                {
                    return result;
                }

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
        /// Checks if a URL is reachable and returns OK
        /// </summary>
        /// <param name="url">URL to check</param>
        /// <param name="allowRedirects">If true, a redirect will count as ok.</param>
        /// <param name="sameUrl">
        /// When true the method only returns true, if the response url didn't change.
        /// </param>
        /// <param name="wrongTitle">
        /// Returns false, if the website has this title. Is used to detect certain redirects.
        /// </param>
        /// <param name="checkForContent">Content to check for</param>
        /// <returns>True, if the URL is reachable</returns>
        internal static bool IsUrlOk(string url, UrlLoadMethod method = UrlLoadMethod.Load, bool allowRedirects = true, bool sameUrl = false, string wrongTitle = "", string checkForContent = "")
        {
            var linkCheckResult = CheckUrl(url, method, allowRedirects, checkForContent);

            return !linkCheckResult.ErrorDetails.Any() && (sameUrl
                       ? linkCheckResult.StatusCode == HttpStatusCode.OK && linkCheckResult.ResponseUrl == url
                       : linkCheckResult.StatusCode == HttpStatusCode.OK) &&
                   (wrongTitle == string.Empty || linkCheckResult.PageTitle != wrongTitle);
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
        /// Loads the HTML document using an offscreen browser instance. This is slower, but can
        /// handle JavaScript heavy sites and mimics a human user best. We don't get a StatusCode
        /// though, so the validity must be checked in the document content individually.
        /// </summary>
        /// <param name="url">url to load</param>
        /// <param name="checkForContent">
        /// Content to check for. If the document doesn't contain this content, it's considered the
        /// wrong page.
        /// </param>
        /// <returns>
        /// The HTML source, response url, status code and an object containing a possible exception
        /// </returns>
        private static UrlLoadResult LoadHtmlDocumentFromOffscreenView(string url, string checkForContent = "")
        {
            var result = new UrlLoadResult();

            try
            {
                var webViewSettings = new WebViewSettings
                {
                    JavaScriptEnabled = true,
                    UserAgent = AgentString
                };

                using (var webView = API.Instance.WebViews.CreateOffscreenView(webViewSettings))
                {
                    try
                    {
                        webView.NavigateAndWait(url);
                        result.ResponseUrl = webView.GetCurrentAddress();
                        var htmlSource = webView.GetPageSource();
                        webView.Close();

                        result.StatusCode = checkForContent.Length == 0 || htmlSource.Contains(checkForContent) ? HttpStatusCode.OK : HttpStatusCode.NotFound;

                        if (result.StatusCode != HttpStatusCode.OK)
                        {
                            return result;
                        }

                        result.Document = new HtmlDocument();
                        result.Document.LoadHtml(htmlSource);
                    }
                    catch (Exception exception)
                    {
                        CatchError(result, exception, url);
                    }
                }
            }
            catch (Exception exception)
            {
                CatchError(result, exception, url);
            }

            return result;
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