﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
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
        /// <summary>
        /// This is top level entry point for diagnostic calculation from client (VS).
        /// 
        /// This will be called by ServiceHub/JsonRpc framework
        /// </summary>
        public async Task CalculateDiagnosticsAsync(DiagnosticArguments arguments, byte[] solutionChecksum, string streamName)
        {
            try
            {
                // entry point for diagnostic service
                var solution = await RoslynServices.SolutionService.GetSolutionAsync(new Checksum(solutionChecksum), CancellationToken).ConfigureAwait(false);
                var projectId = arguments.GetProjectId();
                var analyzers = await GetHostAnalyzerReferences(arguments.GetHostAnalyzerChecksums()).ConfigureAwait(false);

                var result = await (new DiagnosticComputer(solution.GetProject(projectId))).GetDiagnosticsAsync(
                    analyzers, arguments.AnalyzerIds, arguments.ReportSuppressedDiagnostics, arguments.LogAnalyzerExecutionTime, CancellationToken).ConfigureAwait(false);

                await SerializeDiagnosticResultAsync(streamName, result).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // stream to send over result has closed before we
                // had chance to check cancellation
            }
            catch (OperationCanceledException)
            {
                // rpc connection has closed.
                // this can happen if client side cancelled the
                // operation
            }
        }

        private async Task<List<AnalyzerReference>> GetHostAnalyzerReferences(IEnumerable<Checksum> checksums)
        {
            var analyzers = new List<AnalyzerReference>();
            foreach (var checksum in checksums)
            {
                analyzers.Add(await RoslynServices.AssetService.GetAssetAsync<AnalyzerReference>(checksum, CancellationToken).ConfigureAwait(false));
            }

            return analyzers;
        }

        private async Task SerializeDiagnosticResultAsync(string streamName, DiagnosticAnalysisResultMap<string, DiagnosticAnalysisResultBuilder> result)
        {
            using (var stream = await DirectStream.GetAsync(streamName, CancellationToken).ConfigureAwait(false))
            {
                using (var writer = new ObjectWriter(stream))
                {
                    DiagnosticResultSerializer.Serialize(writer, result, CancellationToken);
                }

                await stream.FlushAsync(CancellationToken).ConfigureAwait(false);
            }
        }
    }
}
