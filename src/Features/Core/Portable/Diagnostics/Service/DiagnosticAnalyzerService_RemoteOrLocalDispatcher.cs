// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics;

// this part contains the methods that will attempt to call out to OOP to do the work, and fall back
// to processing locally if it is not available (or we are already in OOP).

internal sealed partial class DiagnosticAnalyzerService : IDiagnosticAnalyzerService
{
    public async Task<ImmutableArray<DiagnosticData>> ForceRunCodeAnalysisDiagnosticsAsync(
        Project project, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var descriptors = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableArray<DiagnosticData>>(
                project,
                (service, solution, cancellationToken) => service.ForceRunCodeAnalysisDiagnosticsAsync(
                    solution, project.Id, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            return descriptors.HasValue ? descriptors.Value : [];
        }

        // Otherwise, fallback to computing in proc.
        return await ForceRunCodeAnalysisDiagnosticsInProcessAsync(project, cancellationToken).ConfigureAwait(false);
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

    public async Task<ImmutableArray<string>> GetCompilationEndDiagnosticDescriptorIdsAsync(
        Solution solution, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var result = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableArray<string>>(
                solution,
                (service, solution, cancellationToken) => service.GetCompilationEndDiagnosticDescriptorIdsAsync(
                    solution, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : [];
        }

        using var _1 = PooledHashSet<string>.GetInstance(out var builder);
        using var _2 = PooledHashSet<(object Reference, string Language)>.GetInstance(out var seenAnalyzerReferencesByLanguage);

        foreach (var project in solution.Projects)
        {
            var analyzersPerReferenceMap = solution.SolutionState.Analyzers.CreateDiagnosticAnalyzersPerReference(project);
            foreach (var (analyzerReference, analyzers) in analyzersPerReferenceMap)
            {
                if (!seenAnalyzerReferencesByLanguage.Add((analyzerReference, project.Language)))
                    continue;

                foreach (var analyzer in analyzers)
                {
                    if (analyzer.IsCompilerAnalyzer())
                        continue;

                    foreach (var buildOnlyDescriptor in _analyzerInfoCache.GetCompilationEndDiagnosticDescriptors(analyzer))
                        builder.Add(buildOnlyDescriptor.Id);
                }
            }
        }

        return builder.ToImmutableArray();
    }

    public async Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>> GetDiagnosticDescriptorsPerReferenceAsync(
        Solution solution, ProjectId? projectId, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var map = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptorData>>>(
                solution,
                (service, solution, cancellationToken) => service.GetDiagnosticDescriptorsPerReferenceAsync(
                    solution, projectId, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            if (!map.HasValue)
                return ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>.Empty;

            return map.Value.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.SelectAsArray(d => d.ToDiagnosticDescriptor()));
        }

        return solution.SolutionState.Analyzers.GetDiagnosticDescriptorsPerReference(
            this._analyzerInfoCache, solution.GetProject(projectId));
    }

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
        Project project, ImmutableArray<DocumentId> documentIds, ImmutableHashSet<string>? diagnosticIds, AnalyzerFilter analyzerFilter, bool includeLocalDocumentDiagnostics, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var result = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableArray<DiagnosticData>>(
                project,
                (service, solution, cancellationToken) => service.GetDiagnosticsForIdsAsync(
                    solution, project.Id, documentIds, diagnosticIds, analyzerFilter, includeLocalDocumentDiagnostics, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            return result.HasValue ? result.Value : [];
        }

        return await GetDiagnosticsForIdsInProcessAsync(
            project, documentIds, diagnosticIds,
            analyzerFilter,
            includeLocalDocumentDiagnostics,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
        Project project,
        ImmutableHashSet<string>? diagnosticIds,
        AnalyzerFilter analyzerFilter,
        CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var result = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableArray<DiagnosticData>>(
                project,
                (service, solution, cancellationToken) => service.GetProjectDiagnosticsForIdsAsync(
                    solution, project.Id, diagnosticIds, analyzerFilter, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            return result.HasValue ? result.Value : [];
        }

        return await GetProjectDiagnosticsForIdsInProcessAsync(
            project, diagnosticIds, analyzerFilter, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> IsAnyDiagnosticIdDeprioritizedAsync(
        Project project, ImmutableArray<string> diagnosticIds, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var result = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, bool>(
                project,
                (service, solution, cancellationToken) => service.IsAnyDiagnosticIdDeprioritizedAsync(
                    solution, project.Id, diagnosticIds, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            return result.HasValue && result.Value;
        }

        return await IsAnyDeprioritizedDiagnosticIdInProcessAsync(
            project, diagnosticIds, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
        TextDocument document,
        TextSpan? range,
        DiagnosticIdFilter diagnosticIdFilter,
        CodeActionRequestPriority? priority,
        DiagnosticKind diagnosticKind,
        CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var result = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableArray<DiagnosticData>>(
                document.Project,
                (service, solution, cancellationToken) => service.GetDiagnosticsForSpanAsync(
                    solution, document.Id, range, diagnosticIdFilter, priority, diagnosticKind, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            return result.HasValue ? result.Value : [];
        }

        return await GetDiagnosticsForSpanInProcessAsync(
            document, range, diagnosticIdFilter, priority, diagnosticKind, cancellationToken).ConfigureAwait(false);
    }
}
