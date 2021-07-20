// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.CodeAnalysis.UnusedReferences.ProjectAssets;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteUnusedReferenceAnalysisService : BrokeredServiceBase, IRemoteUnusedReferenceAnalysisService
    {
        internal sealed class Factory : FactoryBase<IRemoteUnusedReferenceAnalysisService>
        {
            protected override IRemoteUnusedReferenceAnalysisService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteUnusedReferenceAnalysisService(arguments);
        }

        public RemoteUnusedReferenceAnalysisService(ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public ValueTask<ImmutableArray<SerializableReferenceInfo>> GetUnusedReferencesAsync(PinnedSolutionInfo solutionInfo, string projectFilePath, string projectAssetsFilePath, ImmutableArray<SerializableReferenceInfo> projectReferenceInfos, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                if (solution is null)
                {
                    throw new InvalidOperationException();
                }

                var projectReferences = projectReferenceInfos.SelectAsArray(info => info.Rehydrate());

                var references = await ProjectAssetsFileReader.ReadReferencesAsync(projectReferences, projectAssetsFilePath).ConfigureAwait(false);

                var unusedReferences = await UnusedReferencesRemover.GetUnusedReferencesAsync(solution, projectFilePath, references, cancellationToken).ConfigureAwait(false);
                return unusedReferences.SelectAsArray(SerializableReferenceInfo.Dehydrate);
            }, cancellationToken);
        }
    }
}
