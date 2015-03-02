using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace AzureMLClientSDK
{
    public class ServiceManagerCaller
    {
        private const string BaseUrlTemplate = "https://management.azureml.net/workspaces/{0}/";

        private readonly MLAccessContext accessContext;
        private readonly string baseUrl;

        public ServiceManagerCaller(MLAccessContext accessContext)
        {
            this.accessContext = accessContext;
            this.baseUrl = string.Format(BaseUrlTemplate, accessContext.WorkspaceId);
        }

        public async Task<WebServiceEndpoint[]> GetWebServiceEndpoints(string webServiceGroupId)
        {
            string url = this.baseUrl + string.Format("webservices/{0}/endpoints", webServiceGroupId);

            string endpointsJson = await this.CallAzureMLSM(HttpMethod.Get, url, null);

            WebServiceEndpoint[] we = JsonConvert.DeserializeObject<WebServiceEndpoint[]>(endpointsJson);

            return we;
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
