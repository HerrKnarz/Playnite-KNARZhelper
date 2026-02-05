using KNARZhelper.WebCommon.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KNARZhelper.WebCommon
{
    public enum DocumentType
    {
        /// <summary>
        /// Retrieves the source code of the page. Usually used for scraping data from the HTML.
        /// </summary>
        Source = 0,

        /// <summary>
        /// Retrieves the text of the page. Usually used for fetching API results in JSON or XML format.
        /// </summary>
        Text = 1,

        /// <summary>
        /// Doesn't load the content of the page and returns an empty string. Usually used when only
        /// checking if a page is reachable.
        /// </summary>
        Empty = 2
    }

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

        public T GetJsonFromApi<T>(string apiUrl, string apiName, bool debugMode = false)
        {
            try
            {
                var linkCheckResult = LoadUrl(apiUrl, DocumentType.Text, string.Empty, debugMode);

                if (linkCheckResult.StatusCode != HttpStatusCode.OK)
                {
                    Log.Error(new Exception(linkCheckResult.ErrorDetails), $"Error loading data from {apiName} - {apiUrl} - Status code: {linkCheckResult.StatusCode}");
                    return default;
                }

                return JsonConvert.DeserializeObject<T>(linkCheckResult.PageText);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error loading data from {apiName} - {apiUrl}");
            }

            return default;
        }

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
            try
            {
                var linkCheckResult = LoadUrl(url, DocumentType.Empty, checkForContent, debugMode);

                return !linkCheckResult.ErrorDetails.Any() && (sameUrl
                           ? linkCheckResult.StatusCode == HttpStatusCode.OK && linkCheckResult.ResponseUrl == url
                           : linkCheckResult.StatusCode == HttpStatusCode.OK) &&
                       (wrongTitle == string.Empty || linkCheckResult.PageTitle != wrongTitle);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error checking url {url}");
                return false;
            }
        }

        public UrlLoadResult LoadUrl(string url, DocumentType documentType = DocumentType.Empty, string checkForContent = "", bool debugMode = false)
        {
            var result = new UrlLoadResult();
            var ts = DateTime.Now;

            if (debugMode)
            {
                Log.Debug($"Started loading url {url}.");
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

                var pageText = string.Empty;

                pageText = documentType == DocumentType.Text ? _webView.GetPageText() : _webView.GetPageSource();

                _webView.Close();

                if (pageText != null)
                {
                    result.PageTitle = Regex.Match(pageText, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value;

                    if (documentType != DocumentType.Empty)
                    {
                        result.PageText = pageText;
                    }
                }

                if (result.StatusCode != HttpStatusCode.OK)
                {
                    return result;
                }

                if (checkForContent.Length > 0)
                {
                    result.StatusCode = pageText.Contains(checkForContent) ? HttpStatusCode.OK : HttpStatusCode.NotFound;

                    if (result.StatusCode != HttpStatusCode.OK)
                    {
                        return result;
                    }
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
                    Log.Debug($"Finished loading url {url} - status code {result.StatusCode} / duration: ({(DateTime.Now - ts).TotalMilliseconds} ms)  / response url: {result.ResponseUrl} / title: {result.PageTitle}.");
                }
            }
        }
    }
}