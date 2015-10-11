using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace AzureMLClientSDK
{
    public class ServiceManagerCaller
    {
        // AzureML management URL's in 3 regions:
        private static string[] managementUrls = new string[] 
        {
            "management.azureml.net",
            "asiasoutheast.management.azureml.net",
            "europewest.management.azureml.net"
        };

        private const string BaseUrlTemplate = "https://{0}/workspaces/{1}/";

        private readonly MLAccessContext accessContext;

        public ServiceManagerCaller(MLAccessContext accessContext)
        {
            this.accessContext = accessContext;
        }

        public async Task<WebServiceEndpoint[]> GetWebServiceEndpoints(string webServiceGroupId)
        {
            Exception lastException = null;

            // need to find the region where the web service is running, iterate over management URL's
            foreach (string managementUrl in managementUrls)
            {
                string baseUrl = string.Format(BaseUrlTemplate, managementUrl, accessContext.WorkspaceId);
                string url = baseUrl + string.Format("webservices/{0}/endpoints", webServiceGroupId);
                Console.WriteLine("Looking for the web service in {0}", url);

                try
                {
                    string endpointsJson = await this.CallAzureMLSM(HttpMethod.Get, url, null);
                    WebServiceEndpoint[] we = JsonConvert.DeserializeObject<WebServiceEndpoint[]>(endpointsJson);
                    Console.WriteLine("Web service endpoint found {0}", endpointsJson);
                    return we;
                }
                catch(Exception ex)
                {
                    if (ex.Message.Contains("WorkspaceNotFound"))
                    {
                        // workspace may be in another region
                        lastException = ex;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            throw lastException;
        }

        private async Task<string> CallAzureMLSM(HttpMethod method, string url, string payload)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.accessContext.WorkspaceAccessToken);

                HttpRequestMessage req = new HttpRequestMessage(method, url);

                if (!string.IsNullOrWhiteSpace(payload))
                {
                    req.Content = new StringContent(payload);
                    req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                }

                HttpResponseMessage response = await client.SendAsync(req);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return result;
                }
                else
                {
                    throw new HttpRequestException(string.Format("{0}\n{1}", result, response.StatusCode));
                }
            }
        }
    }
}
