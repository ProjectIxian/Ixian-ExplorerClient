using IxianExplorerClient.Meta;
using IXICore;
using IXICore.Meta;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Text.Json;

namespace IxianExplorerClient.API
{
    public class APIClient
    {

        public static string? mapUrlToExplorerAPI(Uri? requestUrl)
        {
            if (requestUrl == null) return null;

            string relativePath = requestUrl.AbsolutePath.TrimStart('/'); // "blocks/1234"
            string query = requestUrl.Query; // "?param=value"

            return $"{Config.explorerAPIBaseUrl}/{relativePath}{query}";
        }

        public static async Task<JsonResponse> forwardRequest(HttpListenerRequest originalRequest, string targetUrl)
        {
            JsonResponse response = new JsonResponse();

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpRequestMessage proxyRequest = new HttpRequestMessage
                    {
                        Method = new HttpMethod(originalRequest.HttpMethod),
                        RequestUri = new Uri(targetUrl)
                    };

                    proxyRequest.Headers.Add("API-KEY", Config.explorerAPIKey);
                    if (originalRequest.HasEntityBody)
                    {
                        using (StreamReader reader = new StreamReader(originalRequest.InputStream))
                        {
                            string content = await reader.ReadToEndAsync();
                            proxyRequest.Content = new StringContent(content, Encoding.UTF8, "application/json");
                        }
                    }

                    // Send the request to the Explorer API
                    HttpResponseMessage proxyResponse = await httpClient.SendAsync(proxyRequest);
                    string responseContent = await proxyResponse.Content.ReadAsStringAsync();

                    if (proxyResponse.IsSuccessStatusCode)
                    {
                        response.result = JsonConvert.DeserializeObject(responseContent)!;
                    }
                    else
                    {
                        response.error = new JsonError
                        {
                            code = (int)proxyResponse.StatusCode,
                            message = $"API Error: {responseContent}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                response.error = new JsonError
                {
                    code = 500,
                    message = $"Unexpected error: {ex.Message}"
                };
            }

            return response;
        }


        public static IxiNumber getAmountByAddressAsync(string address_string)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("API-KEY", Config.explorerAPIKey);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{Config.explorerAPIBaseUrl}/addresses/{address_string}");
                HttpResponseMessage response = httpClient.Send(request);

                response.EnsureSuccessStatusCode(); // Throw exception if status code is not successful
                string content = response.Content.ReadAsStringAsync().Result;

                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    JsonElement root = doc.RootElement;
                    string amountString = root.GetProperty("amount").GetString()!;
                    IxiNumber amount = new(amountString);
                    return amount;
                }
            }
            catch (Exception ex)
            {
                Logging.warn($"Error while fetching wallet balance for {address_string}: {ex.Message}");
                return 0;
            }
        }
    }
}
