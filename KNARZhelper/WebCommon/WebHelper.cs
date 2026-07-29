using KNARZhelper.WebCommon.Models;
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

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
    }
}
