﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
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

    public ValueTask<ImmutableArray<DiagnosticData>> ProduceProjectDiagnosticsAsync(
        Checksum solutionChecksum, ProjectId projectId,
        ImmutableHashSet<string> analyzerIds,
        ImmutableHashSet<string>? diagnosticIds,
        ImmutableArray<DocumentId> documentIds,
        bool includeLocalDocumentDiagnostics,
        bool includeNonLocalDocumentDiagnostics,
        bool includeProjectNonLocalResult,
        CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var project = solution.GetRequiredProject(projectId);
                var service = (DiagnosticAnalyzerService)solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();

                var allProjectAnalyzers = service.GetProjectAnalyzers(project);

                return await service.ProduceProjectDiagnosticsAsync(
                    project, allProjectAnalyzers.FilterAnalyzers(analyzerIds), diagnosticIds, documentIds,
                    includeLocalDocumentDiagnostics, includeNonLocalDocumentDiagnostics, includeProjectNonLocalResult,
                    cancellationToken).ConfigureAwait(false);
            },
            cancellationToken);
    }

    public ValueTask<ImmutableArray<DiagnosticData>> ForceAnalyzeProjectAsync(
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
                return await service.ForceAnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken);
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

    public ValueTask<ImmutableHashSet<string>> GetDeprioritizationCandidatesAsync(
        Checksum solutionChecksum, ProjectId projectId, ImmutableHashSet<string> analyzerIds, CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var project = solution.GetRequiredProject(projectId);
                var service = (DiagnosticAnalyzerService)solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();

                var allProjectAnalyzers = service.GetProjectAnalyzers(project);

                var candidates = await service.GetDeprioritizationCandidatesAsync(
                    project, allProjectAnalyzers.FilterAnalyzers(analyzerIds), cancellationToken).ConfigureAwait(false);

                return candidates.Select(c => c.GetAnalyzerId()).ToImmutableHashSet();
            },
            cancellationToken);
    }

    public ValueTask<ImmutableArray<DiagnosticData>> ComputeDiagnosticsAsync(
        Checksum solutionChecksum, DocumentId documentId, TextSpan? range,
        ImmutableHashSet<string> allAnalyzerIds,
        ImmutableHashSet<string> syntaxAnalyzersIds,
        ImmutableHashSet<string> semanticSpanAnalyzersIds,
        ImmutableHashSet<string> semanticDocumentAnalyzersIds,
        bool incrementalAnalysis,
        bool logPerformanceInfo,
        CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var document = solution.GetRequiredTextDocument(documentId);
                var service = (DiagnosticAnalyzerService)solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();

                var allProjectAnalyzers = service.GetProjectAnalyzers(document.Project);

                return await service.ComputeDiagnosticsAsync(
                    document, range,
                    allProjectAnalyzers.FilterAnalyzers(allAnalyzerIds),
                    allProjectAnalyzers.FilterAnalyzers(syntaxAnalyzersIds),
                    allProjectAnalyzers.FilterAnalyzers(semanticSpanAnalyzersIds),
                    allProjectAnalyzers.FilterAnalyzers(semanticDocumentAnalyzersIds),
                    incrementalAnalysis, logPerformanceInfo, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken);
    }
}
