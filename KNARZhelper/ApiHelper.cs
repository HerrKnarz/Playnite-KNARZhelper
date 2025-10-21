using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KNARZhelper
{
    internal static class ApiHelper
    {
        /// <summary>
        ///     Gets a JSON result from an API and deserializes it.
        /// </summary>
        /// <typeparam name="T">Type the JSON gets deserialized to</typeparam>
        /// <param name="apiUrl">Url to fetch the JSON result from</param>
        /// <param name="apiName">API name for the error message</param>
        /// <param name="encoding">the encoding to use</param>
        /// <param name="useWebView">Uses an OffScreenView when this is true. When false a WebClient is used.</param>
        /// <param name="body">the body to send to the api</param>
        /// <returns>Deserialized JSON result</returns>
        internal static T GetJsonFromApi<T>(string apiUrl, string apiName, Encoding encoding = null, bool useWebView = false, string body = "")
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

                    return JsonConvert.DeserializeObject<T>(pageSource);
                }
                else
                {
                    return GetJsonFromApiAsync<T>(apiUrl, apiName, encoding, body).Result;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error loading data from {apiName} - {apiUrl}");
            }

            return default;
        }

        /// <summary>
        ///     Gets a JSON result from an API and deserializes it.
        /// </summary>
        /// <typeparam name="T">Type the JSON gets deserialized to</typeparam>
        /// <param name="apiUrl">Url to fetch the JSON result from</param>
        /// <param name="apiName">API name for the error message</param>
        /// <param name="encoding">the encoding to use</param>
        /// <param name="body">the body to send to the api</param>
        /// <returns>Deserialized JSON result</returns>
        internal static async Task<T> GetJsonFromApiAsync<T>(string apiUrl, string apiName, Encoding encoding = null, string body = "")
        {
            try
            {
                var pageSource = string.Empty;

                if (encoding is null)
                {
                    encoding = Encoding.Default;
                }

                var client = new WebClient { Encoding = encoding };

                client.Headers.Add("Accept", "application/json");
                client.Headers.Add("user-agent", "Playnite LinkUtilities AddOn");

                var uri = new Uri(apiUrl);

                if (body.Length == 0)
                {
                    pageSource = await client.DownloadStringTaskAsync(uri);
                }
                else
                {
                    client.Headers.Add("Content-Type", "application/json");
                    pageSource = await client.UploadStringTaskAsync(uri, body);
                }

                return JsonConvert.DeserializeObject<T>(pageSource);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error loading data from {apiName} - {apiUrl}");
            }

            return default;
        }
    }
}
