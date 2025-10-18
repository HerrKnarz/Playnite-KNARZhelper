using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Net;
using System.Text;

namespace KNARZhelper
{
    internal static class ApiHelper
    {
        /// <summary>
        ///     Gets a JSON result from an API and deserializes it.
        /// </summary>
        /// <typeparam name="T">Type the JSON gets deserialized to</typeparam>
        /// <param name="apiUrl">Url to fetch the JSON result from</param>
        /// <param name="ApiName">API name for the error message</param>
        /// <param name="encoding">the encoding to use</param>
        /// <returns>Deserialized JSON result</returns>
        internal static T GetJsonFromApi<T>(string apiUrl, string ApiName, Encoding encoding = null, bool useWebView = false, string body = "")
        {
            try
            {
                var pageSource = string.Empty;

                if (useWebView)
                {
                    using (var webView = API.Instance.WebViews.CreateOffscreenView())
                    {
                        webView.NavigateAndWait(apiUrl);
                        pageSource = webView.GetPageText();
                        webView.Close();
                    }
                }
                else
                {
                    if (encoding is null)
                    {
                        encoding = Encoding.Default;
                    }

                    var client = new WebClient { Encoding = encoding };

                    client.Headers.Add("Accept", "application/json");
                    client.Headers.Add("user-agent", "Playnite LinkUtilities AddOn");

                    if (body.Length == 0)
                    {
                        pageSource = client.DownloadString(apiUrl);
                    }
                    else
                    {
                        client.Headers.Add("Content-Type", "application/json");
                        pageSource = client.UploadString(apiUrl, body);
                    }
                }

                return JsonConvert.DeserializeObject<T>(pageSource);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error loading data from {ApiName} - {apiUrl}");
            }

            return default;
        }
    }
}
