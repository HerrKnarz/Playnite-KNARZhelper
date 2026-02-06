using KNARZhelper.WebCommon.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Linq;
using System.Net;
using System.Threading;

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
        private readonly IWebView _webView;

        public LinkWorker(int id)
        {
            Id = id;

            var webViewSettings = new WebViewSettings
            {
                JavaScriptEnabled = true,
                UserAgent = WebHelper.AgentString,

                ResourceLoadedCallback = (callback) =>
                {
                    if (RequestUrl == callback.Request.Url)
                    {
                        try
                        {
                            UrlLoadResult.StatusCode = (HttpStatusCode)callback.Response.StatusCode;
                            UrlLoadResult.RequestUrl = callback.Request.Url;
                            Log.Debug($"Worker {Id} - url {RequestUrl}: 3. ResourceLoadedCallback - callback url: {callback.Request.Url} / status code: {UrlLoadResult.StatusCode}");
                        }
                        catch (Exception ex)
                        {
                            UrlLoadResult.StatusCode = HttpStatusCode.Unused;
                            UrlLoadResult.ErrorDetails = ex.Message;
                        }
                    }
                },
            };

            _webView = API.Instance.WebViews.CreateOffscreenView(webViewSettings);
        }

        public int Id { get; } = 0;
        public string RequestUrl { get; set; } = string.Empty;
        public UrlLoadResult UrlLoadResult { get; set; } = new UrlLoadResult();

        public void Dispose() => _webView?.Dispose();

        public T GetJsonFromApi<T>(string apiUrl, string apiName, bool debugMode = false)
        {
            try
            {
                var linkCheckResult = LoadUrl(apiUrl, DocumentType.Text, debugMode);

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
        /// <param name="debugMode">When true debug messages will be logged</param>
        /// <param name="checkForContent">Content to check for</param>
        /// <returns>True, if the URL is reachable</returns>
        public bool IsUrlOk(string url, bool sameUrl = false, string wrongTitle = "", bool debugMode = false, string checkForContent = "")
        {
            try
            {
                var linkCheckResult = LoadUrl(url, DocumentType.Empty, debugMode, checkForContent);

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

        public UrlLoadResult LoadUrl(string url, DocumentType documentType = DocumentType.Source, bool debugMode = false, string checkForContent = "")
        {
            var ts = DateTime.Now;
            var pageText = string.Empty;
            UrlLoadResult = new UrlLoadResult();

            if (debugMode)
            {
                Log.Debug($"Worker {Id} - url {url}: 1. Started loading url.");
            }

            try
            {
                RequestUrl = url;

                _webView.NavigateAndWait(url);
                UrlLoadResult.ResponseUrl = _webView.GetCurrentAddress();
                pageText = documentType == DocumentType.Text ? _webView.GetPageText() : _webView.GetPageSource();

                // We wait 100 ms to let the callback catch up
                Thread.Sleep(100);
                _webView.Close();

                if (debugMode)
                {
                    Log.Debug($"Worker {Id} - url {url}: 2. NavigateAndWait - duration: ({(DateTime.Now - ts).TotalMilliseconds} ms)");
                    Log.Debug($"Worker {Id} - url {url}: 4. Callback: / status code {UrlLoadResult.StatusCode} / Request url: {UrlLoadResult.RequestUrl}");
                    ts = DateTime.Now;
                }

                if (pageText != null)
                {
                    UrlLoadResult.PageTitle = WebHelper.GetPageTitle(pageText);

                    if (documentType != DocumentType.Empty)
                    {
                        UrlLoadResult.PageText = pageText;
                    }
                }

                if (UrlLoadResult.StatusCode != HttpStatusCode.OK)
                {
                    return UrlLoadResult;
                }

                if (checkForContent.Length > 0)
                {
                    UrlLoadResult.StatusCode = pageText.Contains(checkForContent) ? HttpStatusCode.OK : HttpStatusCode.ExpectationFailed;

                    if (UrlLoadResult.StatusCode != HttpStatusCode.OK)
                    {
                        return UrlLoadResult;
                    }
                }

                return UrlLoadResult;
            }
            catch (Exception ex)
            {
                WebHelper.CatchError(UrlLoadResult, ex, url);

                return UrlLoadResult;
            }
            finally
            {
                if (debugMode)
                {
                    Log.Debug($"Worker {Id} - url {url}: 5. Finished loading - status code {UrlLoadResult.StatusCode} / duration: ({(DateTime.Now - ts).TotalMilliseconds} ms)  / response url: {UrlLoadResult.ResponseUrl} / title: {UrlLoadResult.PageTitle}.");
                }
            }
        }
    }
}