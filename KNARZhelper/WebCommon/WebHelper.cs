using HtmlAgilityPack;
using KNARZhelper.WebCommon.Models;
using Playnite.SDK;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace KNARZhelper.WebCommon
{
    public enum UrlLoadMethod
    {
        /// <summary>
        /// Loads only the header of the URL. This is fast, but many websites return 403 without a real browser.
        /// </summary>
        Header,
        /// <summary>
        /// Loads the URL via the simple Load method of HtmlAgilityPack
        /// </summary>
        Load,
        /// <summary>
        /// Loads the URL using HtmlAgilityPack via a browser instance. This is slower, but can handle
        /// more complex sites. It causes some websites to time out though.
        /// </summary>
        LoadFromBrowser,
        /// <summary>
        /// Loads the URL using an offscreen browser instance. This is slower, but can handle
        /// more complex sites. We don't get a StatusCode though, so the validity must be checked
        /// in the document content individually.
        /// </summary>
        OffscreenView
    }

    public static class WebHelper
    {
        private static bool _allowRedirects = true;

        public static readonly string AgentString =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";

        /// <summary>
        ///     Creates a new instance of HtmlWeb with some default settings.
        /// </summary>
        /// <param name="allowRedirects">If true, redirects are allowed</param>
        /// <returns>Configured HtmlWeb instance</returns>
        private static HtmlWeb GetHtmlWeb(bool allowRedirects)
        {
            var web = new HtmlWeb
            {
                UseCookies = true,
                BrowserTimeout = new TimeSpan(0, 0, 10),
                UserAgent = AgentString
            };

            if (allowRedirects)
            {
                _allowRedirects = allowRedirects;
                web.PreRequest = OnPreRequest;
            }

            return web;
        }

        /// <summary>
        /// Loads the HTML document via the simple Load method of HtmlAgilityPack
        /// </summary>
        /// <param name="url">url to load</param>
        /// <param name="allowRedirects">If true, redirects are allowed</param>
        /// <param name="checkForContent">Content to check for. If the document doesn't contain this content, it's considered the wrong page.</param>
        /// <returns>The document, response url, status code and an object containing a possible exception</returns>
        private static (HtmlAgilityPack.HtmlDocument, string, HttpStatusCode, object) LoadHtmlDocumentSimple(string url, bool allowRedirects = false, string checkForContent = "")
        {
            HtmlAgilityPack.HtmlDocument document = null;
            string responseUrl = null;
            var statusCode = HttpStatusCode.OK;
            object exception = null;

            try
            {
                var htmlWeb = GetHtmlWeb(allowRedirects);
                htmlWeb.BrowserTimeout = new TimeSpan(0, 0, 10);

                document = htmlWeb.Load(url);

                statusCode = htmlWeb.StatusCode == HttpStatusCode.OK
                    ? checkForContent.Length == 0 || document.DocumentNode.InnerHtml.Contains(checkForContent) ? htmlWeb.StatusCode : HttpStatusCode.NotFound
                    : htmlWeb.StatusCode;

                responseUrl = htmlWeb.ResponseUri.AbsoluteUri;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            return (document, responseUrl, statusCode, exception);
        }

        /// <summary>
        /// Loads the HTML document using HtmlAgilityPack via a browser instance. This is slower, but can handle JavaScript heavy sites.
        /// </summary>
        /// <param name="url">url to load</param>
        /// <param name="allowRedirects">If true, redirects are allowed</param>
        /// <param name="checkForContent">Content to check for. If the document doesn't contain this content, it's considered the wrong page.</param>
        /// <returns>The document, response url, status code and an object containing a possible exception</returns>
        private static (HtmlAgilityPack.HtmlDocument, string, HttpStatusCode, object) LoadHtmlDocumentFromBrowser(string url, bool allowRedirects = false, string checkForContent = "")
        {
            object doc = null;
            string responseUrl = null;
            var statusCode = HttpStatusCode.OK;
            object exception = null;

            var thread = new Thread(
                      () =>
                      {
                          try
                          {
                              var web = GetHtmlWeb(allowRedirects);
                              web.BrowserTimeout = new TimeSpan(0, 0, 10);

                              doc = checkForContent.Length > 0
                                  ? web.LoadFromBrowser(url, o =>
                                  {
                                      using (var webBrowser = (WebBrowser)o)
                                      {
                                          return webBrowser.Document.Body.InnerHtml.Contains(checkForContent);
                                      }
                                  })
                                  : web.LoadFromBrowser(url);

                              statusCode = web.StatusCode;
                              responseUrl = web.ResponseUri.AbsoluteUri;
                          }
                          catch (Exception ex)
                          {
                              exception = ex;
                          }
                      });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!thread.Join(new TimeSpan(0, 0, 15)))
            {
                thread.Abort();
            }

            return (doc as HtmlAgilityPack.HtmlDocument, responseUrl, statusCode, exception);
        }

        /// <summary>
        /// Loads the HTML document using an offscreen browser instance. This is slower, but can handle JavaScript heavy sites and mimics a human
        /// user best. We don't get a StatusCode though, so the validity must be checked in the document content individually.
        /// </summary>
        /// <param name="url">url to load</param>
        /// <param name="checkForContent">Content to check for. If the document doesn't contain this content, it's considered the wrong page.</param>
        /// <returns>The HTML source, response url, status code and an object containing a possible exception</returns>
        private static (string, string, HttpStatusCode, object) LoadHtmlDocumentFromOffscreenView(string url, string checkForContent = "")
        {
            string responseUrl = null;
            string htmlSource = null;
            var statusCode = HttpStatusCode.OK;
            object exception = null;

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
                        responseUrl = webView.GetCurrentAddress();
                        htmlSource = webView.GetPageSource();
                        webView.Close();
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }

                statusCode = checkForContent.Length == 0 || htmlSource.Contains(checkForContent) ? HttpStatusCode.OK : HttpStatusCode.NotFound;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            return (htmlSource, responseUrl, statusCode, exception);
        }

        /// <summary>
        ///     PreRequest event for the HtmlWeb class. Is used to disable redirects,
        /// </summary>
        /// <param name="request">The request to be executed</param>
        /// <returns>True, if the request can be executed.</returns>
        private static bool OnPreRequest(HttpWebRequest request)
        {
            request.AllowAutoRedirect = _allowRedirects;
            return true;
        }

        /// <summary>
        ///     tries to reach a URL and returns response infos like status code.
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
        public static (string, HttpStatusCode, object) CheckUrlSimple(string url, bool allowRedirects = false)
        {
            string responseUrl = null;
            var statusCode = HttpStatusCode.OK;
            object exception = null;

            try
            {
                var request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "HEAD";
                request.AllowAutoRedirect = allowRedirects;
                request.UserAgent = AgentString;
                request.Timeout = 10000;
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    statusCode = response.StatusCode;
                    responseUrl = response.ResponseUri.AbsoluteUri;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            return (responseUrl, statusCode, exception);
        }

        /// <summary>
        ///     Removes the scheme of a URL and adds a missing trailing slash. Is used to compare URLs with different schemes
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
        ///     Checks if a URL is reachable and returns OK
        /// </summary>
        /// <param name="url">URL to check</param>
        /// <param name="allowRedirects">If true, a redirect will count as ok.</param>
        /// <param name="sameUrl">When true the method only returns true, if the response url didn't change.</param>
        /// <param name="wrongTitle">Returns false, if the website has this title. Is used to detect certain redirects.</param>
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
        ///     Loads an HTML document from a URL using the specified method.
        /// </summary>
        /// <param name="url">URL to load</param>
        /// <param name="method">Loading method</param>
        /// <param name="allowRedirects">If true, redirects are allowed</param>
        /// <param name="needDocument">
        ///     If true, the loaded document will be returned in the result. Set to false if you only want to check for validity
        ///     and don't need the actual document</param>
        /// <param name="checkForContent">
        ///     Content to check for. Is used to determine if the returned document is valid. For LoadFromBrowser it also is used
        ///     to determine if the document is fully loaded</param>
        /// <returns>Loading result</returns>
        public static UrlLoadResult LoadHtmlDocument(string url, UrlLoadMethod method = UrlLoadMethod.Load, bool allowRedirects = false, bool needDocument = true, string checkForContent = "")
        {
            var result = new UrlLoadResult();

            try
            {
                HtmlAgilityPack.HtmlDocument document = null;
                object exception = null;

                switch (method)
                {
                    case UrlLoadMethod.Header:
                        (result.ResponseUrl, result.StatusCode, exception) = CheckUrlSimple(url, allowRedirects);
                        break;
                    case UrlLoadMethod.Load:
                        (document, result.ResponseUrl, result.StatusCode, exception) = LoadHtmlDocumentSimple(url, allowRedirects, checkForContent);
                        break;
                    case UrlLoadMethod.LoadFromBrowser:
                        (document, result.ResponseUrl, result.StatusCode, exception) = LoadHtmlDocumentFromBrowser(url, allowRedirects, checkForContent);
                        break;
                    default:
                        {
                            var htmlSource = string.Empty;

                            (htmlSource, result.ResponseUrl, result.StatusCode, exception) = LoadHtmlDocumentFromOffscreenView(url, checkForContent);

                            if (result.StatusCode != HttpStatusCode.OK)
                            {
                                break;
                            }

                            try
                            {
                                document = new HtmlAgilityPack.HtmlDocument();
                                document.LoadHtml(htmlSource);
                            }
                            catch (Exception ex)
                            {
                                exception = ex;
                            }

                            break;
                        }
                }

                if (exception is WebException webEx)
                {
                    if (webEx.Response != null)
                    {
                        var response = webEx.Response;
                        var dataStream = response.GetResponseStream();

                        if (dataStream != null)
                        {
                            var reader = new StreamReader(dataStream);
                            result.ErrorDetails = reader.ReadToEnd();
                            result.StatusCode = ((HttpWebResponse)response).StatusCode;
                            reader.Close();

                            Log.Debug($"Error loading url {url} => status code {result.StatusCode}");
                        }
                    }

                    return result;
                }
                else if (exception is Exception ex)
                {
                    result.ErrorDetails = ex.Message;
                    result.StatusCode = HttpStatusCode.BadRequest;
                    Log.Error(ex, $"Error loading url {url} => {result.ErrorDetails}");

                    return result;
                }

                if (method == UrlLoadMethod.Header)
                {
                    return result;
                }

                if (document == null)
                {
                    result.ErrorDetails = $"Error loading HTML document from {url}";
                    result.StatusCode = HttpStatusCode.BadRequest;
                    return result;
                }

                result.PageTitle = document?.DocumentNode?.SelectSingleNode("html/head/title")?.InnerText.Trim();

                if (needDocument)
                {
                    result.Document = document;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorDetails = ex.Message;
                result.StatusCode = HttpStatusCode.BadRequest;
                Log.Error(ex, $"Error loading HTML document from {url}");

                return result;
            }
        }
    }
}
