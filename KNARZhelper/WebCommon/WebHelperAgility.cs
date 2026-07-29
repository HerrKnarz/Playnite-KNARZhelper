using HtmlAgilityPack;
using KNARZhelper.WebCommon.Models;
using System;
using System.Net;
using System.Threading.Tasks;

namespace KNARZhelper.WebCommon
{
    public class WebHelperAgility
    {
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
        public static UrlLoadResult LoadHtmlDocument(string url, bool allowRedirects = false, bool needDocument = true, string checkForContent = "") => AsyncHelper.RunSync(async () => await LoadHtmlDocumentAsync(url, allowRedirects, needDocument, checkForContent));

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

                if (result.PageText.IsNullOrEmpty())
                {
                    result.ErrorDetails = $"Error loading HTML document from {url}";
                    result.StatusCode = HttpStatusCode.BadRequest;
                    return result;
                }

                result.PageTitle = WebHelper.GetPageTitle(result.PageText);
            }
            catch (Exception exception)
            {
                WebHelper.CatchError(result, exception, url);
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
                UserAgent = WebHelper.AgentString
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

                result.PageText = htmlWeb.Load(url).DocumentNode.InnerHtml;

                var httpWebResponse = await tcs.Task;

                result.StatusCode = httpWebResponse.StatusCode == HttpStatusCode.OK
                    ? checkForContent.Length == 0 || result.PageText.Contains(checkForContent) ? httpWebResponse.StatusCode : HttpStatusCode.NotFound
                    : httpWebResponse.StatusCode;

                result.ResponseUrl = httpWebResponse?.ResponseUri?.AbsoluteUri;
            }
            catch (Exception exception)
            {
                WebHelper.CatchError(result, exception, url);
            }

            return result;
        }
    }
}
