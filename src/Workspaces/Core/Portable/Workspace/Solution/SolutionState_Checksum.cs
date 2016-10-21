// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        public bool TryGetStateChecksums(out SolutionStateChecksums stateChecksums)
        {
            return _lazyChecksums.TryGetValue(out stateChecksums);
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
                var projectChecksumTasks = ProjectIds.Select(id => ProjectStates[id].GetChecksumAsync(cancellationToken));

                var serializer = new Serializer(_solutionServices.Workspace.Services);
                var infoChecksum = serializer.CreateChecksum(SolutionInfo.Attributes, cancellationToken);

                var projectChecksums = await Task.WhenAll(projectChecksumTasks).ConfigureAwait(false);
                return new SolutionStateChecksums(infoChecksum, new ProjectChecksumCollection(projectChecksums));
            }
        }
    }
}
