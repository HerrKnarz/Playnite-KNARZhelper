using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KNARZhelper
{
    internal static class ApiHelper
    {
        /// <summary>
        /// Gets a JSON result from an API and deserializes it.
        /// </summary>
        /// <typeparam name="T">Type the JSON gets deserialized to</typeparam>
        /// <param name="apiUrl">Url to fetch the JSON result from</param>
        /// <param name="apiName">API name for the error message</param>
        /// <param name="encoding">the encoding to use</param>
        /// <param name="body">the body to send to the api</param>
        /// <returns>Deserialized JSON result</returns>
        internal static T GetJsonFromApi<T>(string apiUrl, string apiName, Encoding encoding = null, string body = "")
        {
            try
            {
                return GetJsonFromApiAsync<T>(apiUrl, apiName, encoding, body).Result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error loading data from {apiName} - {apiUrl}");
            }

            return default;
        }

        /// <summary>
        /// Gets a JSON result from an API and deserializes it.
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

                var task = default(Task<string>);

                if (body.Length == 0)
                {
                    task = client.DownloadStringTaskAsync(uri);
                }
                else
                {
                    client.Headers.Add("Content-Type", "application/json");

                    task = client.UploadStringTaskAsync(uri, body);
                }

                pageSource = await Task.WhenAny(task, Task.Delay(10000)) == task
                    ? task.Result
                    : throw new Exception(
                        $"Timeout loading data from {apiName} - {apiUrl}");

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