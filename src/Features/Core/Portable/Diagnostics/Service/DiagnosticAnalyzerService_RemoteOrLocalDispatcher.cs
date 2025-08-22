// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics;

// this part contains the methods that will attempt to call out to OOP to do the work, and fall back
// to processing locally if it is not available (or we are already in OOP).

internal sealed partial class DiagnosticAnalyzerService : IDiagnosticAnalyzerService
{
    public async Task<ImmutableArray<DiagnosticData>> ForceAnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var result = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableArray<DiagnosticData>>(
                project,
                (service, solution, cancellationToken) => service.ForceAnalyzeProjectAsync(solution, project.Id, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : [];
        }

        // No OOP connection. Compute in proc.
        return await ForceAnalyzeProjectInProcessAsync(project, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<DiagnosticDescriptor>> GetDiagnosticDescriptorsAsync(
        Solution solution, ProjectId projectId, AnalyzerReference analyzerReference, string language, CancellationToken cancellationToken)
    {
        // Attempt to compute this OOP.
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client is not null &&
            analyzerReference is AnalyzerFileReference analyzerFileReference)
        {
            var descriptors = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableArray<DiagnosticDescriptorData>>(
                solution,
                (service, solution, cancellationToken) => service.GetDiagnosticDescriptorsAsync(solution, projectId, analyzerFileReference.FullPath, language, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            if (!descriptors.HasValue)
                return [];

            return descriptors.Value.SelectAsArray(d => d.ToDiagnosticDescriptor());
        }

        // Otherwise, fallback to computing in proc.
        return analyzerReference
            .GetAnalyzers(language)
            .SelectManyAsArray(this._analyzerInfoCache.GetDiagnosticDescriptors);
    }

    public async Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>> GetDiagnosticDescriptorsPerReferenceAsync(Solution solution, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var map = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptorData>>>(
                solution,
                (service, solution, cancellationToken) => service.GetDiagnosticDescriptorsPerReferenceAsync(solution, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            if (!map.HasValue)
                return ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>.Empty;

            return map.Value.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.SelectAsArray(d => d.ToDiagnosticDescriptor()));
        }

        return solution.SolutionState.Analyzers.GetDiagnosticDescriptorsPerReference(this._analyzerInfoCache);
    }

    public async Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>> GetDiagnosticDescriptorsPerReferenceAsync(Project project, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var map = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptorData>>>(
                project,
                (service, solution, cancellationToken) => service.GetDiagnosticDescriptorsPerReferenceAsync(solution, project.Id, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            if (!map.HasValue)
                return ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>.Empty;

            return map.Value.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.SelectAsArray(d => d.ToDiagnosticDescriptor()));
        }

        return project.Solution.SolutionState.Analyzers.GetDiagnosticDescriptorsPerReference(this._analyzerInfoCache, project);
    }

    public async Task<ImmutableArray<DiagnosticAnalyzer>> GetDeprioritizationCandidatesAsync(
        Project project, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var analyzerIds = analyzers.Select(a => a.GetAnalyzerId()).ToImmutableHashSet();
            var result = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableHashSet<string>>(
                project,
                (service, solution, cancellationToken) => service.GetDeprioritizationCandidatesAsync(
                    solution, project.Id, analyzerIds, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            if (!result.HasValue)
                return [];

            return analyzers.FilterAnalyzers(result.Value);
        }

        using var _ = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var builder);

        var hostAnalyzerInfo = GetOrCreateHostAnalyzerInfo(project);
        var compilationWithAnalyzers = await GetOrCreateCompilationWithAnalyzersAsync(
            project, analyzers, hostAnalyzerInfo, this.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);

        foreach (var analyzer in analyzers)
        {
            if (await IsCandidateForDeprioritizationBasedOnRegisteredActionsAsync(analyzer).ConfigureAwait(false))
                builder.Add(analyzer);
        }

        return builder.ToImmutableAndClear();

        async Task<bool> IsCandidateForDeprioritizationBasedOnRegisteredActionsAsync(DiagnosticAnalyzer analyzer)
        {
            // We deprioritize SymbolStart/End and SemanticModel analyzers from 'Normal' to 'Low' priority bucket,
            // as these are computationally more expensive.
            // Note that we never de-prioritize compiler analyzer, even though it registers a SemanticModel action.
            if (compilationWithAnalyzers == null ||
                analyzer.IsWorkspaceDiagnosticAnalyzer() ||
                analyzer.IsCompilerAnalyzer())
            {
                return false;
            }

            var telemetryInfo = await compilationWithAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, cancellationToken).ConfigureAwait(false);
            if (telemetryInfo == null)
                return false;

            return telemetryInfo.SymbolStartActionsCount > 0 || telemetryInfo.SemanticModelActionsCount > 0;
        }
    }

    internal async Task<ImmutableArray<DiagnosticData>> ProduceProjectDiagnosticsAsync(
        Project project,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        ImmutableHashSet<string>? diagnosticIds,
        ImmutableArray<DocumentId> documentIds,
        bool includeLocalDocumentDiagnostics,
        bool includeNonLocalDocumentDiagnostics,
        bool includeProjectNonLocalResult,
        CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var analyzerIds = analyzers.Select(a => a.GetAnalyzerId()).ToImmutableHashSet();
            var result = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableArray<DiagnosticData>>(
                project,
                (service, solution, cancellationToken) => service.ProduceProjectDiagnosticsAsync(
                    solution, project.Id, analyzerIds, diagnosticIds, documentIds,
                    includeLocalDocumentDiagnostics, includeNonLocalDocumentDiagnostics, includeProjectNonLocalResult,
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);
            if (!result.HasValue)
                return [];

            return result.Value;
        }

        // Fallback to proccessing in proc.
        return await ProduceProjectDiagnosticsInProcessAsync(
            project, analyzers, diagnosticIds, documentIds,
            includeLocalDocumentDiagnostics,
            includeNonLocalDocumentDiagnostics,
            includeProjectNonLocalResult,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<DiagnosticData>> ComputeDiagnosticsAsync(
        TextDocument document,
        TextSpan? range,
        ImmutableArray<DiagnosticAnalyzer> allAnalyzers,
        ImmutableArray<DiagnosticAnalyzer> syntaxAnalyzers,
        ImmutableArray<DiagnosticAnalyzer> semanticSpanAnalyzers,
        ImmutableArray<DiagnosticAnalyzer> semanticDocumentAnalyzers,
        bool incrementalAnalysis,
        bool logPerformanceInfo,
        CancellationToken cancellationToken)
    {
        if (allAnalyzers.Length == 0)
            return [];

        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var allAnalyzerIds = allAnalyzers.Select(a => a.GetAnalyzerId()).ToImmutableHashSet();
            var syntaxAnalyzersIds = syntaxAnalyzers.Select(a => a.GetAnalyzerId()).ToImmutableHashSet();
            var semanticSpanAnalyzersIds = semanticSpanAnalyzers.Select(a => a.GetAnalyzerId()).ToImmutableHashSet();
            var semanticDocumentAnalyzersIds = semanticDocumentAnalyzers.Select(a => a.GetAnalyzerId()).ToImmutableHashSet();

            var result = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableArray<DiagnosticData>>(
                document.Project,
                (service, solution, cancellationToken) => service.ComputeDiagnosticsAsync(
                    solution, document.Id, range,
                    allAnalyzerIds, syntaxAnalyzersIds, semanticSpanAnalyzersIds, semanticDocumentAnalyzersIds,
                    incrementalAnalysis, logPerformanceInfo, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : [];
        }

        return await ComputeDiagnosticsInProcessAsync(
            document, range, allAnalyzers, syntaxAnalyzers, semanticSpanAnalyzers, semanticDocumentAnalyzers,
            incrementalAnalysis, logPerformanceInfo, cancellationToken).ConfigureAwait(false);
    }
}
