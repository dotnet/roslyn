// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.InheritanceMargin;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteInheritanceMarginService : BrokeredServiceBase, IRemoteInheritanceMarginService
    {
        internal sealed class Factory : FactoryBase<IRemoteInheritanceMarginService>
        {
            protected override IRemoteInheritanceMarginService CreateService(in ServiceConstructionArguments arguments)
            {
                return new RemoteInheritanceMarginService(arguments);
            }
        }

        public RemoteInheritanceMarginService(in ServiceConstructionArguments arguments) : base(in arguments)
        {
        }

        public ValueTask<ImmutableArray<SerializableInheritanceMarginItem>> GetInheritanceMarginItemsAsync(
            PinnedSolutionInfo pinnedSolutionInfo,
            ProjectId projectId,
            ImmutableArray<(SymbolKey symbolKey, int lineNumber)> symbolKeyAndLineNumbers,
            CancellationToken cancellationToken)
            => RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(pinnedSolutionInfo, cancellationToken).ConfigureAwait(false);
                return await InheritanceMarginServiceHelper
                    .GetInheritanceMemberItemAsync(solution, projectId, symbolKeyAndLineNumbers, cancellationToken)
                    .ConfigureAwait(false);
            }, cancellationToken);
    }
}
