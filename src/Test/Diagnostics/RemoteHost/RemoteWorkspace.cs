using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// nobody uses this
    /// </summary>
    internal class RemoteWorkspace : Workspace
    {
        public RemoteWorkspace(HostServices host, string workspaceKind) : base(host, workspaceKind)
        {
        }
    }
}
