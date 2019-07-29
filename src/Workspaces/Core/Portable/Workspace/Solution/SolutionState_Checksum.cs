// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        public bool TryGetStateChecksums(out SolutionStateChecksums stateChecksums)
        {
            return _lazyChecksums.TryGetValue(out stateChecksums);
        }

        public Task<SolutionStateChecksums> GetStateChecksumsAsync(CancellationToken cancellationToken)
        {
            return _lazyChecksums.GetValueAsync(cancellationToken);
        }

        public async Task<Checksum> GetChecksumAsync(CancellationToken cancellationToken)
        {
            var collection = await _lazyChecksums.GetValueAsync(cancellationToken).ConfigureAwait(false);
            return collection.Checksum;
        }

        private async Task<SolutionStateChecksums> ComputeChecksumsAsync(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.SolutionState_ComputeChecksumsAsync, FilePath, cancellationToken))
            {
                // get states by id order to have deterministic checksum
                var orderedProjectIds = ChecksumCache.GetOrCreate(ProjectIds, _ => ProjectIds.OrderBy(id => id.Id).ToImmutableArray());
                var projectChecksumTasks = orderedProjectIds.Select(id => ProjectStates[id])
                                                            .Where(s => RemoteSupportedLanguages.IsSupported(s.Language))
                                                            .Select(s => s.GetChecksumAsync(cancellationToken));

                var serializer = _solutionServices.Workspace.Services.GetService<ISerializerService>();
                var infoChecksum = serializer.CreateChecksum(SolutionAttributes, cancellationToken);

                var projectChecksums = await Task.WhenAll(projectChecksumTasks).ConfigureAwait(false);
                return new SolutionStateChecksums(infoChecksum, new ProjectChecksumCollection(projectChecksums));
            }
        }
    }
}
