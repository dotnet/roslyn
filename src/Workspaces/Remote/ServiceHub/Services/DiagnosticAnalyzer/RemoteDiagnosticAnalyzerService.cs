﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Telemetry;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteDiagnosticAnalyzerService(in BrokeredServiceBase.ServiceConstructionArguments arguments)
    : BrokeredServiceBase(arguments), IRemoteDiagnosticAnalyzerService
{
    internal sealed class Factory : FactoryBase<IRemoteDiagnosticAnalyzerService>
    {
        protected override IRemoteDiagnosticAnalyzerService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteDiagnosticAnalyzerService(arguments);
    }

    private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache = new();

    /// <summary>
    /// Calculate diagnostics. this works differently than other ones such as todo comments or designer attribute scanner
    /// since in proc and out of proc runs quite differently due to concurrency and due to possible amount of data
    /// that needs to pass through between processes
    /// </summary>
    public async ValueTask<SerializableDiagnosticAnalysisResults> CalculateDiagnosticsAsync(Checksum solutionChecksum, DiagnosticArguments arguments, CancellationToken cancellationToken)
    {
        // Complete RPC right away so the client can start reading from the stream.
        // The fire-and forget task starts writing to the output stream and the client will read it until it reads all expected data.

        using (TelemetryLogging.LogBlockTimeAggregatedHistogram(FunctionId.PerformAnalysis_Summary, $"Total"))
        using (RoslynLogger.LogBlock(FunctionId.CodeAnalysisService_CalculateDiagnosticsAsync, arguments.ProjectId.DebugName, cancellationToken))
        {
            return await RunWithSolutionAsync(
                solutionChecksum,
                async solution =>
                {
                    var documentId = arguments.DocumentId;
                    var projectId = arguments.ProjectId;
                    var project = solution.GetRequiredProject(projectId);
                    var document = arguments.DocumentId != null
                        ? solution.GetTextDocument(arguments.DocumentId) ?? await solution.GetSourceGeneratedDocumentAsync(arguments.DocumentId, cancellationToken).ConfigureAwait(false)
                        : null;
                    var documentSpan = arguments.DocumentSpan;
                    var documentAnalysisKind = arguments.DocumentAnalysisKind;
                    var hostWorkspaceServices = this.GetWorkspace().Services;

                    var result = await DiagnosticComputer.GetDiagnosticsAsync(
                        document, project, solutionChecksum,
                        documentSpan,
                        arguments.ProjectAnalyzerIds, arguments.HostAnalyzerIds, documentAnalysisKind,
                        _analyzerInfoCache, hostWorkspaceServices,
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

    public ValueTask<ImmutableArray<DiagnosticData>> GetSourceGeneratorDiagnosticsAsync(Checksum solutionChecksum, ProjectId projectId, CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var project = solution.GetRequiredProject(projectId);
                var diagnostics = await project.GetSourceGeneratorDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
                using var builder = TemporaryArray<DiagnosticData>.Empty;
                foreach (var diagnostic in diagnostics)
                {
                    var document = solution.GetDocument(diagnostic.Location.SourceTree);
                    var data = document != null
                        ? DiagnosticData.Create(diagnostic, document)
                        : DiagnosticData.Create(diagnostic, project);
                    builder.Add(data);
                }

                return builder.ToImmutableAndClear();
            }, cancellationToken);
    }

    public ValueTask ReportAnalyzerPerformanceAsync(ImmutableArray<AnalyzerPerformanceInfo> snapshot, int unitCount, bool forSpanAnalysis, CancellationToken cancellationToken)
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

                service.AddSnapshot(snapshot, unitCount, forSpanAnalysis);
            }

            return default;
        }, cancellationToken);
    }

    public ValueTask<ImmutableArray<DiagnosticDescriptorData>> GetDiagnosticDescriptorsAsync(Checksum solutionChecksum, ProjectId projectId, string analyzerReferenceFullPath, CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var project = solution.GetRequiredProject(projectId);
                var analyzerReference = project.AnalyzerReferences
                    .First(r => r.FullPath == analyzerReferenceFullPath);

                var descriptors = await project.GetDiagnosticDescriptorsAsync(analyzerReference, cancellationToken).ConfigureAwait(false);
                var descriptorData = descriptors.SelectAsArray(DiagnosticDescriptorData.Create);

                return descriptorData;
            },
            cancellationToken);
    }

    public ValueTask<ImmutableArray<DiagnosticDescriptorData>> GetDiagnosticDescriptorsAsync(
        Checksum solutionChecksum,
        ProjectId projectId,
        string analyzerReferenceFullPath,
        string language,
        CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var service = solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
                var project = solution.GetRequiredProject(projectId);
                var analyzerReference = project.AnalyzerReferences
                    .First(r => r.FullPath == analyzerReferenceFullPath);

                var descriptors = await service.GetDiagnosticDescriptorsAsync(
                    solution, projectId, analyzerReference, language, cancellationToken).ConfigureAwait(false);
                return descriptors.SelectAsArray(DiagnosticDescriptorData.Create);
            },
            cancellationToken);
    }

    public ValueTask<ImmutableDictionary<ImmutableArray<string>, ImmutableArray<DiagnosticDescriptorData>>> GetLanguageKeyedDiagnosticDescriptorsAsync(
        Checksum solutionChecksum,
        ProjectId projectId,
        string analyzerReferenceFullPath,
        CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var service = solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
                var project = solution.GetRequiredProject(projectId);
                var analyzerReference = project.AnalyzerReferences
                    .First(r => r.FullPath == analyzerReferenceFullPath);

                var map = await service.GetLanguageKeyedDiagnosticDescriptorsAsync(
                    solution, projectId, analyzerReference, cancellationToken).ConfigureAwait(false);
                return map.ToImmutableDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.SelectAsArray(DiagnosticDescriptorData.Create));
            },
            cancellationToken);
    }

    public ValueTask<ImmutableDictionary<string, DiagnosticDescriptorData>> TryGetDiagnosticDescriptorsAsync(
        Checksum solutionChecksum,
        ImmutableArray<string> diagnosticIds,
        CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var service = solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
                var map = await service.TryGetDiagnosticDescriptorsAsync(solution, diagnosticIds, cancellationToken).ConfigureAwait(false);
                return map.ToImmutableDictionary(
                    kvp => kvp.Key,
                    kvp => DiagnosticDescriptorData.Create(kvp.Value));
            },
            cancellationToken);
    }

    public ValueTask<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptorData>>> GetDiagnosticDescriptorsPerReferenceAsync(
        Checksum solutionChecksum,
        CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var service = solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
                var map = await service.GetDiagnosticDescriptorsPerReferenceAsync(solution, cancellationToken).ConfigureAwait(false);
                return map.ToImmutableDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.SelectAsArray(DiagnosticDescriptorData.Create));

            },
            cancellationToken);
    }

    public ValueTask<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptorData>>> GetDiagnosticDescriptorsPerReferenceAsync(
        Checksum solutionChecksum,
        ProjectId projectId,
        CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var project = solution.GetRequiredProject(projectId);
                var service = solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
                var map = await service.GetDiagnosticDescriptorsPerReferenceAsync(project, cancellationToken).ConfigureAwait(false);
                return map.ToImmutableDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.SelectAsArray(DiagnosticDescriptorData.Create));

            },
            cancellationToken);
    }
}
