using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingWorkspaceWrapper
    {
        internal UnitTestingWorkspaceWrapper(Workspace underlyingObject)
            => UnderlyingObject = underlyingObject ?? throw new ArgumentNullException(nameof(underlyingObject));

        internal Workspace UnderlyingObject { get; }

        public async Task<UnitTestingRemoteHostClientExtensionWrapper> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
        {
            var remoteHostClient = await UnderlyingObject.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            return new UnitTestingRemoteHostClientExtensionWrapper(remoteHostClient);
        }
    }
}
