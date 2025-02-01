using IxianExplorerClient.Meta;
using IXICore;
using IXICore.Meta;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Text.Json;
using static IXICore.Transaction;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
                using HttpClient httpClient = new();
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
                Logging.warn($"Fetching wallet balance for {address_string}: {ex.Message}");
                return 0;
            }
        }

        public static int getTransactionCountByAddressAsync(string address_string)
        {
            try
            {
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("API-KEY", Config.explorerAPIKey);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{Config.explorerAPIBaseUrl}/addresses/{address_string}");
                HttpResponseMessage response = httpClient.Send(request);

                response.EnsureSuccessStatusCode(); // Throw exception if status code is not successful
                string content = response.Content.ReadAsStringAsync().Result;

                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    JsonElement root = doc.RootElement;
                    int txcount = root.GetProperty("txcount").GetInt32();
                    return txcount;
                }
            }
            catch (Exception ex)
            {
                Logging.warn($"Fetching wallet balance for {address_string}: {ex.Message}");
                return 0;
            }
        }

        private static void processTransaction(JsonElement transactionElement, string addressString)
        {
            try
            {
                int activity_type = -1;

                string txid = transactionElement.GetProperty("txid").GetString()!;
                int type = int.Parse(transactionElement.GetProperty("type").GetString()!);
                string data = transactionElement.GetProperty("data").GetString()!;
                byte[] dataBytes = Convert.FromBase64String(data);
                string amount = transactionElement.GetProperty("amount").GetString()!;
                long timestamp = long.Parse(transactionElement.GetProperty("timestamp").GetString()!);
                ulong applied = transactionElement.GetProperty("applied").GetUInt64();
                int version = transactionElement.GetProperty("version").GetInt32();

                // Deserialize 'from'
                string fromRaw = transactionElement.GetProperty("from").GetString()!;
                IDictionary<Address, ToEntry> fromList = JsonSerializer.Deserialize<Dictionary<string, string>>(fromRaw)!
                    .ToDictionary(
                        kv => new Address(kv.Key),
                        kv => new ToEntry(version, new IxiNumber(kv.Value))
                    );

                // Deserialize 'to'
                string toRaw = transactionElement.GetProperty("to").GetString()!;
                IDictionary<Address, ToEntry> toList = JsonSerializer.Deserialize<Dictionary<string, string>>(toRaw)!
                    .ToDictionary(
                        kv => new Address(kv.Key),
                        kv => new ToEntry(version, new IxiNumber(kv.Value))
                    );
                string fee = transactionElement.GetProperty("fee").GetString()!;

                Address primary_address = fromList.First().Key;

                Dictionary<byte[], List<byte[]>> from_wallet_list = null;
                Dictionary<byte[], List<byte[]>> to_wallet_list = null;
                to_wallet_list = IxianHandler.extractMyAddressesFromAddressList(toList);
                if (to_wallet_list != null)
                {
                    activity_type = (int)ActivityType.TransactionReceived;
                }
                else
                {
                    // Scan the fromList
                    from_wallet_list = IxianHandler.extractMyAddressesFromAddressList(fromList);

                    if (from_wallet_list != null)
                    {
                        activity_type = (int)ActivityType.TransactionSent;
                        primary_address = new Address(from_wallet_list.First().Value.First());
                        amount = fromList.First().Value.amount.ToString();
                    }
                }

                // Skip if not received or sent type transaction
                if (activity_type == -1)
                    return;

                int status = (int)ActivityStatus.Final;

                if (to_wallet_list != null)
                {
                    // Received
                    foreach (var extractedWallet in to_wallet_list)
                    {
                        foreach (var waddress in extractedWallet.Value)
                        {
                            Address addr = new Address(waddress);
                            foreach (var entry in toList)
                            {
                                if (addr.addressNoChecksum.SequenceEqual(entry.Key.addressNoChecksum))
                                {
                                    IxiNumber toAmount = entry.Value.amount;
                                    Activity activity = new Activity(extractedWallet.Key,
                                                                    addr.ToString(),
                                                                    primary_address.ToString(),
                                                                    toList,
                                                                    activity_type,
                                                                    Convert.FromBase64String(data),
                                                                    toAmount.ToString(),
                                                                    timestamp,
                                                                    status,
                                                                    applied,
                                                                    txid);

                                    ActivityStorage.insertActivity(activity);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Sent
                    Address wallet = primary_address;
                    Activity activity = new Activity(IxianHandler.getWalletStorageBySecondaryAddress(primary_address).getSeedHash(),
                                                    wallet.ToString(),
                                                    primary_address.ToString(),
                                                    toList,
                                                    activity_type,
                                                    Convert.FromBase64String(data),
                                                    amount,
                                                    timestamp,
                                                    status,
                                                    applied,
                                                    txid);
                    ActivityStorage.insertActivity(activity);
                }
            }
            catch (Exception ex)
            {
                Logging.warn($"Failed to process transaction for address {addressString}: {ex.Message}");
            }
        }

        public static bool getTransactionsByAddressAsync(string addressString, int page = 1)
        {
            try
            {
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("API-KEY", Config.explorerAPIKey);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{Config.explorerAPIBaseUrl}/addresses/{addressString}/transactions?page={page}");
                HttpResponseMessage response = httpClient.Send(request);

                response.EnsureSuccessStatusCode(); // Throw exception if status code is not successful
                string content = response.Content.ReadAsStringAsync().Result;

                Address address = new Address(addressString);

                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    JsonElement root = doc.RootElement;
                    if (root.ValueKind != JsonValueKind.Array)
                    {
                        Logging.warn($"Unexpected response format for address {addressString}");
                        return false;
                    }

                    foreach (JsonElement transactionElement in root.EnumerateArray())
                    {
                        processTransaction(transactionElement, addressString);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logging.warn($"Fetching transaction activity for {addressString}: {ex.Message}");
                return false;
            }
        }

        public static bool getTransactionUpdatesByAddressAsync(string addressString, string lastTxid)
        {
            try
            {
                using HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("API-KEY", Config.explorerAPIKey);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{Config.explorerAPIBaseUrl}/addresses/{addressString}/updates?lastTx={lastTxid}");
                HttpResponseMessage response = httpClient.Send(request);

                response.EnsureSuccessStatusCode(); // Throw exception if status code is not successful
                string content = response.Content.ReadAsStringAsync().Result;

                Address address = new Address(addressString);

                using (JsonDocument doc = JsonDocument.Parse(content))
                {
                    JsonElement root = doc.RootElement;
                    if (root.ValueKind != JsonValueKind.Array)
                    {
                        Logging.warn($"Unexpected response format for address {addressString}");
                        return false;
                    }

                    foreach (JsonElement transactionElement in root.EnumerateArray())
                    {
                        processTransaction(transactionElement, addressString);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.warn($"Fetching transaction activity for {addressString}: {ex.Message}");             
            }
            return false;
        }
      
    }
}
