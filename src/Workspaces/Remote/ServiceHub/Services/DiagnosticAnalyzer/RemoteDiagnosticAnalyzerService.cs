// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
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

    public ValueTask<ImmutableArray<DiagnosticData>> ForceRunCodeAnalysisDiagnosticsAsync(
        Checksum solutionChecksum, ProjectId projectId, CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var project = solution.GetRequiredProject(projectId);
                var service = solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
                return await service.ForceRunCodeAnalysisDiagnosticsAsync(
                    project, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken);
    }

    public ValueTask<bool> IsAnyDiagnosticIdDeprioritizedAsync(
        Checksum solutionChecksum, ProjectId projectId, ImmutableArray<string> diagnosticIds, CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var project = solution.GetRequiredProject(projectId);
                var service = solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();

                return await service.IsAnyDiagnosticIdDeprioritizedAsync(
                    project, diagnosticIds, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken);
    }

    public ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
        Checksum solutionChecksum, ProjectId projectId,
        ImmutableArray<DocumentId> documentIds,
        ImmutableHashSet<string>? diagnosticIds,
        AnalyzerFilter analyzerFilter,
        bool includeLocalDocumentDiagnostics,
        CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var project = solution.GetRequiredProject(projectId);
                var service = solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();

                return await service.GetDiagnosticsForIdsAsync(
                    project, documentIds, diagnosticIds, analyzerFilter,
                    includeLocalDocumentDiagnostics, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken);
    }

    public ValueTask<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
        Checksum solutionChecksum, ProjectId projectId,
        ImmutableHashSet<string>? diagnosticIds,
        AnalyzerFilter analyzerFilter,
        CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var project = solution.GetRequiredProject(projectId);
                var service = solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();

                return await service.GetProjectDiagnosticsForIdsAsync(
                    project, diagnosticIds, analyzerFilter, cancellationToken).ConfigureAwait(false);
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
        return RunServiceAsync(async cancellationToken =>
        {
            using (RoslynLogger.LogBlock(FunctionId.CodeAnalysisService_ReportAnalyzerPerformance, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var service = GetWorkspace().Services.GetService<IPerformanceTrackerService>();
                if (service == null)
                {
                    return;
                }

                service.AddSnapshot(snapshot, unitCount, forSpanAnalysis);
            }
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

    public ValueTask<ImmutableArray<string>> GetCompilationEndDiagnosticDescriptorIdsAsync(
        Checksum solutionChecksum, CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var service = solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
                return await service.GetCompilationEndDiagnosticDescriptorIdsAsync(
                    solution, cancellationToken).ConfigureAwait(false);
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
        ProjectId? projectId,
        CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var service = solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
                var map = await service.GetDiagnosticDescriptorsPerReferenceAsync(solution, projectId, cancellationToken).ConfigureAwait(false);
                return map.ToImmutableDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.SelectAsArray(DiagnosticDescriptorData.Create));

            },
            cancellationToken);
    }

    public ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
        Checksum solutionChecksum,
        DocumentId documentId,
        TextSpan? range,
        DiagnosticIdFilter diagnosticIdFilter,
        CodeActionRequestPriority? priority,
        DiagnosticKind diagnosticKind,
        CancellationToken cancellationToken)
    {
        return RunWithSolutionAsync(
            solutionChecksum,
            async solution =>
            {
                var document = await solution.GetRequiredTextDocumentAsync(
                    documentId, cancellationToken).ConfigureAwait(false);
                var service = solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();

                return await service.GetDiagnosticsForSpanAsync(
                    document, range, diagnosticIdFilter, priority, diagnosticKind, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken);
    }
}
