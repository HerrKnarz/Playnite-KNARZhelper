using HtmlAgilityPack;
using KNARZhelper.WebCommon.Models;
using Playnite.SDK;
using System;
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

        public UrlLoadResult LoadUrl(string url, bool needDocument = true, string checkForContent = "")
        {
            var result = new UrlLoadResult();

            try
            {
                // TODO: Add some kind of timeout handling

                _webView.NavigateAndWait(url);
                result = _tcs.Task.Result;
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
            }
            catch (Exception ex)
            {
                WebHelper.CatchError(result, ex, url);
            }

            return result;
        }
    }
}