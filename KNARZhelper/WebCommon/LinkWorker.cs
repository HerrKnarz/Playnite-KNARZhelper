using KNARZhelper.WebCommon.Models;
using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
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
        private readonly bool _detailedDebug = false;
        private readonly WebViewSettings _webViewSettings;
#pragma warning disable IDE0090 // Use 'new(...)'
        private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();
#pragma warning restore IDE0090 // Use 'new(...)'
        private IWebView _webView;

        public LinkWorker(int id)
        {
            Id = id;

            _webViewSettings = new WebViewSettings
            {
                JavaScriptEnabled = true,
                UserAgent = WebHelper.AgentString
            };
        }

        public HashSet<string> AllowedCallbackUrls { get; set; } = new HashSet<string>();

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
        public bool IsUrlOk(string url, bool sameUrl = false, string wrongTitle = "", bool debugMode = false, string checkForContent = "", HashSet<string> allowedCallbackUrls = null)
        {
            try
            {
                var linkCheckResult = LoadUrl(url, DocumentType.Empty, debugMode, checkForContent, allowedCallbackUrls);

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

        public UrlLoadResult LoadUrl(string url, DocumentType documentType = DocumentType.Source, bool debugMode = false, string checkForContent = "", HashSet<string> allowedCallbackUrls = null, bool waitForCallback = true, int delay = 0)
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
                if (!url.IsValidHttpUrl())
                {
                    UrlLoadResult.StatusCode = HttpStatusCode.BadRequest;
                    UrlLoadResult.ErrorDetails = "Invalid URL format.";
                    UrlLoadResult.PageTitle = "Invalid URL format.";
                    UrlLoadResult.IsUrlValid = false;
                    return UrlLoadResult;
                }

                Reset();
                RequestUrl = url;
                _tcs = new TaskCompletionSource<bool>();

                if (allowedCallbackUrls != null)
                {
                    AllowedCallbackUrls.UnionWith(allowedCallbackUrls.Select(WebHelper.CleanUpUrl));
                }
                else
                {
                    AllowedCallbackUrls.Clear();
                }

                var loadTask = new Task<bool>(() =>
                {
                    _webView.NavigateAndWait(url);

                    if (delay > 0)
                    {
                        Thread.Sleep(delay);
                    }

                    UrlLoadResult.ResponseUrl = _webView.GetCurrentAddress();
                    pageText = documentType == DocumentType.Text ? _webView.GetPageText() : _webView.GetPageSource();
                    return true;
                });

                loadTask.Start();

                try
                {
                    AsyncHelper.RunSync(async () => await AsyncHelper.TimeoutAfter(loadTask, TimeSpan.FromSeconds(30)));
                }
                catch
                {
                    if (debugMode)
                    {
                        Log.Debug($"Worker {Id} - url {url}: 1. timeout while loading page!");
                    }
                }

                if (debugMode)
                {
                    Log.Debug($"Worker {Id} - url {url}: 3. NavigateAndWait - duration: ({(DateTime.Now - ts).TotalMilliseconds} ms)");
                    ts = DateTime.Now;
                }

                if (waitForCallback)
                {
                    try
                    {
                        AsyncHelper.RunSync(async () => await AsyncHelper.TimeoutAfter(_tcs.Task, TimeSpan.FromSeconds(10)));
                    }
                    catch
                    {
                        if (debugMode)
                        {
                            Log.Debug($"Worker {Id} - url {url}: 2. ResourceLoadedCallback - timeout!");

                            // TODO: Remove notification once I tested it with enough games!
                            API.Instance.Notifications.Add("LinkUtilities",
                            $"ResourceLoadedCallback timeout{Environment.NewLine}Checked url: {url} {Environment.NewLine}Response url: {UrlLoadResult.ResponseUrl}",
                            NotificationType.Info);
                        }
                    }
                }

                _webViewSettings.ResourceLoadedCallback = null;
                _webView.Close();

                if (UrlLoadResult.StatusCode == HttpStatusCode.Unused && UrlLoadResult.ResponseUrl.Any())
                {
                    UrlLoadResult.StatusCode = HttpStatusCode.SeeOther;
                }

                if (debugMode)
                {
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
                AllowedCallbackUrls.Clear();

                if (debugMode)
                {
                    Log.Debug($"Worker {Id} - url {url}: 5. Finished loading - status code {UrlLoadResult.StatusCode} / duration: ({(DateTime.Now - ts).TotalMilliseconds} ms)  / response url: {UrlLoadResult.ResponseUrl} / title: {UrlLoadResult.PageTitle}.");
                }
            }
        }

        internal void Reset()
        {
            _webView?.Close();
            _webView?.Dispose();

            _webViewSettings.ResourceLoadedCallback = WebViewCallback;
            _webView = API.Instance.WebViews.CreateOffscreenView(_webViewSettings);
        }

        private void WebViewCallback(WebViewResourceLoadedCallback callback)
        {
            try
            {
                if (_detailedDebug)
                {
                    Log.Debug($"Worker {Id} - url {RequestUrl}: 2AAA. ResourceLoadedCallback - callback url: {callback.Request.Url} / status code: {(HttpStatusCode)callback.Response.StatusCode}");
                }

                if (WebHelper.CleanUpUrl(RequestUrl) == WebHelper.CleanUpUrl(callback.Request.Url) || AllowedCallbackUrls.Contains(WebHelper.CleanUpUrl(callback.Request.Url)))
                {
                    try
                    {
                        UrlLoadResult.StatusCode = (HttpStatusCode)callback.Response.StatusCode;
                        UrlLoadResult.RequestUrl = callback.Request.Url;
                        Log.Debug($"Worker {Id} - url {RequestUrl}: 2. ResourceLoadedCallback - callback url: {callback.Request.Url} / status code: {UrlLoadResult.StatusCode}");
                        _tcs.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        UrlLoadResult.StatusCode = HttpStatusCode.Unused;
                        UrlLoadResult.ErrorDetails = ex.Message;
                        _tcs.TrySetResult(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Worker {Id} - url {RequestUrl}: 2. ResourceLoadedCallback - error!");
            }
        }
    }
}
