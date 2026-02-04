using HtmlAgilityPack;
using KNARZhelper.WebCommon.Models;
using Playnite.SDK;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace KNARZhelper.WebCommon
{
    public class LinkWorker : IDisposable
    {
        private readonly TaskCompletionSource<UrlLoadResult> _tcs = new TaskCompletionSource<UrlLoadResult>();
        private readonly IWebView _webView;

        public LinkWorker()
        {
            var webViewSettings = new WebViewSettings
            {
                JavaScriptEnabled = true,
                UserAgent = WebHelper.AgentString,

                ResourceLoadedCallback = (callback) =>
                {
                    var result = new UrlLoadResult();

                    try
                    {
                        result.StatusCode = (HttpStatusCode)callback.Response.StatusCode;
                    }
                    catch (Exception ex)
                    {
                        result.StatusCode = HttpStatusCode.Unused;
                        result.ErrorDetails = ex.Message;
                    }

                    _tcs.TrySetResult(result);
                },
            };

            _webView = API.Instance.WebViews.CreateOffscreenView(webViewSettings);
        }

        public void Dispose() => _webView?.Dispose();

        /// <summary>
        /// Checks if a URL is reachable and returns OK
        /// </summary>
        /// <param name="url">URL to check</param>
        /// <param name="sameUrl">
        /// When true the method only returns true, if the response url didn't change.
        /// </param>
        /// <param name="wrongTitle">
        /// Returns false, if the website has this title. Is used to detect certain redirects.
        /// </param>
        /// <param name="checkForContent">Content to check for</param>
        /// <param name="debugMode">When true debug messages will be logged</param>
        /// <returns>True, if the URL is reachable</returns>
        public bool IsUrlOk(string url, bool sameUrl = false, string wrongTitle = "", string checkForContent = "", bool debugMode = false)
        {
            var linkCheckResult = LoadUrl(url, false, checkForContent, debugMode);

            return !linkCheckResult.ErrorDetails.Any() && (sameUrl
                       ? linkCheckResult.StatusCode == HttpStatusCode.OK && linkCheckResult.ResponseUrl == url
                       : linkCheckResult.StatusCode == HttpStatusCode.OK) &&
                   (wrongTitle == string.Empty || linkCheckResult.PageTitle != wrongTitle);
        }

        public UrlLoadResult LoadUrl(string url, bool needDocument = true, string checkForContent = "", bool debugMode = false)
        {
            var result = new UrlLoadResult();
            var ts = DateTime.Now;

            if (debugMode)
            {
                Log.Debug($"Started adding link for url {url}.");
            }

            try
            {
                // TODO: Add some kind of timeout handling. Check how others do it!

                _webView.NavigateAndWait(url);

                if (debugMode)
                {
                    Log.Debug($"NavigateAndWait for url {url} - duration: ({(DateTime.Now - ts).TotalMilliseconds} ms)");
                    ts = DateTime.Now;
                }

                result = _tcs.Task.Result;

                if (debugMode)
                {
                    Log.Debug($"Callback for url {url} - duration: ({(DateTime.Now - ts).TotalMilliseconds} ms)");
                    ts = DateTime.Now;
                }

                result.ResponseUrl = _webView.GetCurrentAddress();
                var htmlSource = _webView.GetPageSource();
                _webView.Close();

                if (result.StatusCode != HttpStatusCode.OK)
                {
                    return result;
                }

                if (checkForContent.Length > 0)
                {
                    result.StatusCode = htmlSource.Contains(checkForContent) ? HttpStatusCode.OK : HttpStatusCode.NotFound;

                    if (result.StatusCode != HttpStatusCode.OK)
                    {
                        return result;
                    }
                }

                result.Document = new HtmlDocument();
                result.Document.LoadHtml(htmlSource);
                result.PageTitle = result.Document?.DocumentNode?.SelectSingleNode("html/head/title")?.InnerText.Trim();

                if (!needDocument)
                {
                    result.Document = null;
                }

                return result;
            }
            catch (Exception ex)
            {
                WebHelper.CatchError(result, ex, url);

                return result;
            }
            finally
            {
                if (debugMode)
                {
                    Log.Debug($"Finished adding link for url {url} - status code {result.StatusCode} / duration: ({(DateTime.Now - ts).TotalMilliseconds} ms)  / response url: {result.ResponseUrl} / title: {result.PageTitle}.");
                }
            }
        }
    }
}