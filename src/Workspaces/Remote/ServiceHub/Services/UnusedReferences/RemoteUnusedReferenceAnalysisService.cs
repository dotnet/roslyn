// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public ValueTask<ImmutableArray<ReferenceInfo>> GetUnusedReferencesAsync(Checksum solutionChecksum, string projectFilePath, string projectAssetsFilePath, ImmutableArray<ReferenceInfo> projectReferences, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                // Read specified references with dependency information from the project assets file.
                var references = await ProjectAssetsFileReader.ReadReferencesAsync(projectReferences, projectAssetsFilePath).ConfigureAwait(false);

                // Determine unused references
                var unusedReferences = await UnusedReferencesRemover.GetUnusedReferencesAsync(solution, projectFilePath, references, cancellationToken).ConfigureAwait(false);

                // Remove dependency information before returning.
                return unusedReferences.SelectAsArray(reference => reference.WithDependencies(null));
            }, cancellationToken);
        }
    }
}
