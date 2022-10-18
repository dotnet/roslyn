// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteDesignerAttributeDiscoveryService : BrokeredServiceBase, IRemoteDesignerAttributeDiscoveryService
    {
        /// <summary>
        /// Allow designer attribute computation to continue on on the server, even while the client is processing the
        /// last batch of results.
        /// </summary>
        /// <remarks>
        /// This value was not determined empirically.
        /// </remarks>
        private const int MaxReadAhead = 64;

        internal sealed class Factory : FactoryBase<IRemoteDesignerAttributeDiscoveryService>
        {
            protected override IRemoteDesignerAttributeDiscoveryService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteDesignerAttributeDiscoveryService(arguments);
        }

        public RemoteDesignerAttributeDiscoveryService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public IAsyncEnumerable<DesignerAttributeData> DiscoverDesignerAttributesAsync(
            Checksum solutionChecksum,
            ProjectId projectId,
            DocumentId? priorityDocument,
            CancellationToken cancellationToken)
        {
            var stream = StreamWithSolutionAsync(
                solutionChecksum,
                (solution, cancellationToken) =>
                {
                    var project = solution.GetRequiredProject(projectId);
                    var service = solution.Services.GetRequiredService<IDesignerAttributeDiscoveryService>();
                    return service.ProcessProjectAsync(project, priorityDocument, cancellationToken);
                }, cancellationToken);
            return stream.WithJsonRpcSettings(new JsonRpcEnumerableSettings { MaxReadAhead = MaxReadAhead });
        }
    }
}
