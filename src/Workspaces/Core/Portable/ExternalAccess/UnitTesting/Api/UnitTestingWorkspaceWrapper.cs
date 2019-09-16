using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingWorkspaceExtensions
    {
        public async static Task<UnitTestingRemoteHostClientExtensionWrapper> TryGetUnitTestingRemoteHostExtensionWrapperAsync(this Workspace workspace, CancellationToken cancellationToken)
        {
            var remoteHostClient = await workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            return new UnitTestingRemoteHostClientExtensionWrapper(remoteHostClient);
        }
    }
}
