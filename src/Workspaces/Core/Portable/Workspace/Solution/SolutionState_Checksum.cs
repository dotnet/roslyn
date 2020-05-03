// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SolutionState
    {
        public bool TryGetStateChecksums(out SolutionStateChecksums stateChecksums)
            => _lazyChecksums.TryGetValue(out stateChecksums);

        public Task<SolutionStateChecksums> GetStateChecksumsAsync(CancellationToken cancellationToken)
            => _lazyChecksums.GetValueAsync(cancellationToken);

        public async Task<Checksum> GetChecksumAsync(CancellationToken cancellationToken)
        {
            var collection = await GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            return collection.Checksum;
        }

        private async Task<SolutionStateChecksums> ComputeChecksumsAsync(CancellationToken cancellationToken)
        {
            try
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
                    var optionsChecksum = serializer.CreateChecksum(Options, cancellationToken);

                    var analyzerReferenceChecksums = ChecksumCache.GetOrCreate<AnalyzerReferenceChecksumCollection>(AnalyzerReferences,
                        _ => new AnalyzerReferenceChecksumCollection(AnalyzerReferences.Select(r => serializer.CreateChecksum(r, cancellationToken)).ToArray()));

                    var projectChecksums = await Task.WhenAll(projectChecksumTasks).ConfigureAwait(false);
                    return new SolutionStateChecksums(infoChecksum, optionsChecksum, new ProjectChecksumCollection(projectChecksums), analyzerReferenceChecksums);
                }
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
