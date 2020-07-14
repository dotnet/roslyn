// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteDiagnosticAnalyzerService
    {
        /// <summary>
        /// Calculate dignostics. this works differently than other ones such as todo comments or designer attribute scanner
        /// since in proc and out of proc runs quite differently due to concurrency and due to possible amount of data
        /// that needs to pass through between processes
        /// </summary>
        public Task CalculateDiagnosticsAsync(PinnedSolutionInfo solutionInfo, DiagnosticArguments arguments, string pipeName, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                // if this analysis is explicitly asked by user, boost priority of this request
                using (RoslynLogger.LogBlock(FunctionId.CodeAnalysisService_CalculateDiagnosticsAsync, arguments.ProjectId.DebugName, cancellationToken))
                using (arguments.ForcedAnalysis ? UserOperationBooster.Boost() : default)
                {
                    var solutionService = CreateSolutionService(solutionInfo);
                    var solution = await solutionService.GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var projectId = arguments.ProjectId;

                    var result = await new DiagnosticComputer(solution.GetProject(projectId), _analyzerInfoCache).GetDiagnosticsAsync(
                        arguments.AnalyzerIds, arguments.ReportSuppressedDiagnostics, arguments.LogAnalyzerExecutionTime, cancellationToken).ConfigureAwait(false);

                    await RemoteEndPoint.WriteDataToNamedPipeAsync(pipeName, result, storage: null, (writer, data, cancellationToken) =>
                    {
                        var (diagnostics, telemetry) = DiagnosticResultSerializer.WriteDiagnosticAnalysisResults(writer, data, cancellationToken);

                        // save log for debugging
                        Log(TraceEventType.Information, $"diagnostics: {diagnostics}, telemetry: {telemetry}");

                        return Task.CompletedTask;
                    }, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public void ReportAnalyzerPerformance(List<AnalyzerPerformanceInfo> snapshot, int unitCount, CancellationToken cancellationToken)
        {
            RunService(() =>
            {
                using (RoslynLogger.LogBlock(FunctionId.CodeAnalysisService_ReportAnalyzerPerformance, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var service = SolutionService.PrimaryWorkspace.Services.GetService<IPerformanceTrackerService>();
                    if (service == null)
                    {
                        return;
                    }

                    service.AddSnapshot(snapshot, unitCount);
                }
            }, cancellationToken);
        }
    }
}
