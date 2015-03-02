using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureMLClientSDK
{
    public class WebServiceEndpoint
    {
        public string Name { get; set; }
        public string CreationTime { get; set; }
        public string WorkspaceId { get; set; }
        public string WebServiceId { get; set; }
        public string HelpLocation { get; set; }
        public string PrimaryKey { get; set; }
        public string SecondaryKey { get; set; }
        public string ApiLocation { get; set; }
        public string Version { get; set; }
        public int MaxConcurrentCalls { get; set; }
        public string DiagnosticsTraceLevel { get; set; }
        public string ThrottleLevel { get; set; }
    }
}
