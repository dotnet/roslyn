// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService
    {
        public async Task CalculateDiagnosticsAsync(byte[] checksum, Guid guid, string debugName, byte[][] hostAnalyzerChecksums, string[] analyzerIds, string streamName)
        {
            // entry point for diagnostic service
            var solutionSnapshotId = await RoslynServices.AssetService.GetAssetAsync<SolutionSnapshotId>(new Checksum(checksum)).ConfigureAwait(false);

            var solution = await RoslynServices.SolutionService.GetSolutionAsync(solutionSnapshotId, CancellationToken).ConfigureAwait(false);
            var projectId = ProjectId.CreateFromSerialized(guid, debugName);
            var analyzers = await GetHostAnalyzerReferences(hostAnalyzerChecksums).ConfigureAwait(false);

            var result = await (new DiagnosticComputer()).GetDiagnosticsAsync(solution, projectId, analyzers, analyzerIds, CancellationToken).ConfigureAwait(false);

            await SerializeDiagnosticResultAsync(streamName, result).ConfigureAwait(false);
        }

        private static async Task<List<AnalyzerReference>> GetHostAnalyzerReferences(byte[][] checksums)
        {
            var analyzers = new List<AnalyzerReference>();
            foreach (var checksum in checksums)
            {
                analyzers.Add(await RoslynServices.AssetService.GetAssetAsync<AnalyzerReference>(new Checksum(checksum)).ConfigureAwait(false));
            }

            return analyzers;
        }

        private async Task SerializeDiagnosticResultAsync(string streamName, DiagnosticResult result)
        {
            using (var stream = new SlaveDirectStream(streamName))
            {
                await stream.ConnectAsync(CancellationToken).ConfigureAwait(false);

                using (var writer = new ObjectWriter(stream))
                {
                    DiagnosticResultSerializer.Serialize(writer, result.AnalysisResult, result.TelemetryInfo, CancellationToken);
                }

                await stream.FlushAsync(CancellationToken).ConfigureAwait(false);

                // TODO: think of a way this is not needed
                // wait for the other side to finish reading data I sent over
                stream.WaitForMaster();
            }
        }
    }
}
