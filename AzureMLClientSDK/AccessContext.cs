using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureMLClientSDK
{
    public abstract class AccessContext
    {
    }

    public sealed class MLAccessContext : AccessContext
    {
        private readonly string workspaceId;

        private readonly string workspaceAccessToken;

        public MLAccessContext(string workspaceId, string workspaceAccessToken)
        {
            this.workspaceId = workspaceId;
            this.workspaceAccessToken = workspaceAccessToken;
        }

        internal string WorkspaceId {  get { return this.workspaceId; } }

        internal string WorkspaceAccessToken {  get { return this.workspaceAccessToken; } }
    }
}
