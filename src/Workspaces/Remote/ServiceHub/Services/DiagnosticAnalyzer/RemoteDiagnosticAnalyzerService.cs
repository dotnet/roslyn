// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteDiagnosticAnalyzerService : BrokeredServiceBase, IRemoteDiagnosticAnalyzerService
    {
        internal sealed class Factory : FactoryBase<IRemoteDiagnosticAnalyzerService>
        {
            protected override IRemoteDiagnosticAnalyzerService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteDiagnosticAnalyzerService(arguments);
        }

        private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache = new();

        public RemoteDiagnosticAnalyzerService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask StartSolutionCrawlerAsync(CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                // register solution crawler:
                var workspace = GetWorkspace();
                workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>().Register(workspace);

                return ValueTaskFactory.CompletedTask;
            }, cancellationToken);
        }

        /// <summary>
        /// Calculate dignostics. this works differently than other ones such as todo comments or designer attribute scanner
        /// since in proc and out of proc runs quite differently due to concurrency and due to possible amount of data
        /// that needs to pass through between processes
        /// </summary>
        public async ValueTask<SerializableDiagnosticAnalysisResults> CalculateDiagnosticsAsync(Checksum solutionChecksum, DiagnosticArguments arguments, CancellationToken cancellationToken)
        {
            // Complete RPC right away so the client can start reading from the stream.
            // The fire-and forget task starts writing to the output stream and the client will read it until it reads all expected data.

            using (RoslynLogger.LogBlock(FunctionId.CodeAnalysisService_CalculateDiagnosticsAsync, arguments.ProjectId.DebugName, cancellationToken))
            {
                return await RunWithSolutionAsync(
                    solutionChecksum,
                    async solution =>
                    {
                        var documentId = arguments.DocumentId;
                        var projectId = arguments.ProjectId;
                        var project = solution.GetProject(projectId);
                        var document = arguments.DocumentId != null
                            ? solution.GetTextDocument(arguments.DocumentId) ?? await solution.GetSourceGeneratedDocumentAsync(arguments.DocumentId, cancellationToken).ConfigureAwait(false)
                            : null;
                        var documentSpan = arguments.DocumentSpan;
                        var documentAnalysisKind = arguments.DocumentAnalysisKind;
                        var diagnosticComputer = new DiagnosticComputer(document, project, arguments.IdeOptions, documentSpan, documentAnalysisKind, _analyzerInfoCache);

                        var result = await diagnosticComputer.GetDiagnosticsAsync(
                            arguments.AnalyzerIds,
                            reportSuppressedDiagnostics: arguments.ReportSuppressedDiagnostics,
                            logPerformanceInfo: arguments.LogPerformanceInfo,
                            getTelemetryInfo: arguments.GetTelemetryInfo,
                            cancellationToken).ConfigureAwait(false);

                        // save log for debugging
                        var diagnosticCount = result.Diagnostics.Sum(
                            entry => entry.diagnosticMap.Syntax.Length + entry.diagnosticMap.Semantic.Length + entry.diagnosticMap.NonLocal.Length + entry.diagnosticMap.Other.Length);

                        Log(TraceEventType.Information, $"diagnostics: {diagnosticCount}, telemetry: {result.Telemetry.Length}");

                        return result;
                    }, cancellationToken).ConfigureAwait(false);
            }
        }

        public ValueTask ReportAnalyzerPerformanceAsync(ImmutableArray<AnalyzerPerformanceInfo> snapshot, int unitCount, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                using (RoslynLogger.LogBlock(FunctionId.CodeAnalysisService_ReportAnalyzerPerformance, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var service = GetWorkspace().Services.GetService<IPerformanceTrackerService>();
                    if (service == null)
                    {
                        return default;
                    }

                    service.AddSnapshot(snapshot, unitCount);
                }

                return default;
            }, cancellationToken);
        }
    }
}
