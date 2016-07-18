// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote.Diagnostics;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal class CodeAnalysisService : ServiceHubJsonRpcServiceBase
    {
        public CodeAnalysisService(Stream stream, IServiceProvider serviceProvider) :
            base(stream, serviceProvider)
        {
        }

        public async Task<string> CalculateDiagnosticsAsync(byte[] checksum, Guid guid, string debugName, byte[][] hostAnalyzerChecksums, string[] analyzerIds)
        {
            // entry point for diagnostic service
            var solutionSnapshotId = await RoslynServices.AssetService.GetAssetAsync<SolutionSnapshotId>(new Checksum(ImmutableArray.Create(checksum))).ConfigureAwait(false);
            var projectId = ProjectId.CreateFromSerialized(guid, debugName);

            var solution = await RoslynServices.SolutionService.GetSolutionAsync(solutionSnapshotId, CancellationToken).ConfigureAwait(false);

            var analyzers = new List<AnalyzerReference>();
            foreach (var analyzerChecksum in hostAnalyzerChecksums)
            {
                analyzers.Add(await RoslynServices.AssetService.GetAssetAsync<AnalyzerReference>(new Checksum(ImmutableArray.Create(analyzerChecksum))).ConfigureAwait(false));
            }

            var result = await (new DiagnosticComputer()).GetDiagnosticsAsync(solution, projectId, analyzers, analyzerIds, CancellationToken).ConfigureAwait(false);

            // just for testing
            return result.AnalysisResult.Count.ToString();
        }
    }
}
