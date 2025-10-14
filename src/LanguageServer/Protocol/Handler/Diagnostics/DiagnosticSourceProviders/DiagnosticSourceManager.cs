// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

[Export(typeof(IDiagnosticSourceManager)), Shared]
internal sealed class DiagnosticSourceManager : IDiagnosticSourceManager
{
    /// <summary>
    /// Document level <see cref="IDiagnosticSourceProvider"/> providers ordered by name.
    /// </summary>
    private readonly ImmutableDictionary<string, IDiagnosticSourceProvider> _nameToDocumentProviderMap;

    /// <summary>
    /// Workspace level <see cref="IDiagnosticSourceProvider"/> providers ordered by name.
    /// </summary>
    private readonly ImmutableDictionary<string, IDiagnosticSourceProvider> _nameToWorkspaceProviderMap;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DiagnosticSourceManager([ImportMany] IEnumerable<IDiagnosticSourceProvider> sourceProviders)
    {
        _nameToDocumentProviderMap = sourceProviders
            .Where(p => p.IsDocument)
            .ToImmutableDictionary(kvp => kvp.Name, kvp => kvp);

        _nameToWorkspaceProviderMap = sourceProviders
            .Where(p => !p.IsDocument)
            .ToImmutableDictionary(kvp => kvp.Name, kvp => kvp);
    }

    public ImmutableArray<string> GetDocumentSourceProviderNames(ClientCapabilities clientCapabilities)
        => _nameToDocumentProviderMap.SelectAsArray(
            predicate: kvp => kvp.Value.IsEnabled(clientCapabilities),
            selector: kvp => kvp.Key);

    public ImmutableArray<string> GetWorkspaceSourceProviderNames(ClientCapabilities clientCapabilities)
        => _nameToWorkspaceProviderMap.SelectAsArray(
            predicate: kvp => kvp.Value.IsEnabled(clientCapabilities),
            selector: kvp => kvp.Key);

    public ValueTask<ImmutableArray<IDiagnosticSource>> CreateDocumentDiagnosticSourcesAsync(RequestContext context, string? providerName, CancellationToken cancellationToken)
        => CreateDiagnosticSourcesAsync(context, providerName, _nameToDocumentProviderMap, isDocument: true, cancellationToken);

    public ValueTask<ImmutableArray<IDiagnosticSource>> CreateWorkspaceDiagnosticSourcesAsync(RequestContext context, string? providerName, CancellationToken cancellationToken)
        => CreateDiagnosticSourcesAsync(context, providerName, _nameToWorkspaceProviderMap, isDocument: false, cancellationToken);

    private static async ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(
        RequestContext context,
        string? providerName,
        ImmutableDictionary<string, IDiagnosticSourceProvider> nameToProviderMap,
        bool isDocument,
        CancellationToken cancellationToken)
    {
        if (providerName != null)
        {
            // VS does not distinguish between document and workspace sources. Thus it can request
            // document diagnostics with workspace source name. We need to handle this case.
            if (nameToProviderMap.TryGetValue(providerName, out var provider))
            {
                Contract.ThrowIfFalse(provider.IsEnabled(context.GetRequiredClientCapabilities()));
                return await provider.CreateDiagnosticSourcesAsync(context, cancellationToken).ConfigureAwait(false);
            }

            return [];
        }
        else
        {
            // Some clients (legacy VS/VSCode, Razor) do not support multiple sources - a null source indicates that diagnostics from all sources should be returned.
            using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var sourcesBuilder);
            foreach (var (name, provider) in nameToProviderMap)
            {
                if (!provider.IsEnabled(context.GetRequiredClientCapabilities()))
                {
                    continue;
                }

                var namedSources = await provider.CreateDiagnosticSourcesAsync(context, cancellationToken).ConfigureAwait(false);
                sourcesBuilder.AddRange(namedSources);
            }

            var sources = sourcesBuilder.ToImmutableAndClear();
            return AggregateSourcesIfNeeded(sources, isDocument);
        }
    }

    public static ImmutableArray<IDiagnosticSource> AggregateSourcesIfNeeded(ImmutableArray<IDiagnosticSource> sources, bool isDocument)
    {
        if (sources.Length <= 1)
        {
            return sources;
        }

        if (isDocument)
        {
            // Group all document sources into a single source.
            sources = [new AggregatedDocumentDiagnosticSource(sources)];
        }
        else
        {
            // We ASSUME that all sources with the same ProjectOrDocumentID and IsLiveSource
            // will have same value for GetDocumentIdentifier and GetProject(). Thus can be
            // aggregated in a single source which will return same values. See
            // AggregatedDocumentDiagnosticSource implementation for more details.
            sources = [.. sources.GroupBy(s => s.GetId(), s => s).SelectMany(g => AggregatedDocumentDiagnosticSource.AggregateIfNeeded(g))];
        }

        return sources;
    }

    /// <summary>
    /// Aggregates multiple <see cref="IDiagnosticSource"/>s into a single source.
    /// </summary>
    /// <param name="sources">Sources to aggregate</param>
    /// <remarks>
    /// Aggregation is usually needed for clients like VS Code which supports single source per request.
    /// </remarks>
    private sealed class AggregatedDocumentDiagnosticSource(ImmutableArray<IDiagnosticSource> sources) : IDiagnosticSource
    {
        public static ImmutableArray<IDiagnosticSource> AggregateIfNeeded(IEnumerable<IDiagnosticSource> sources)
        {
            var result = sources.ToImmutableArray();
            if (result.Length > 1)
            {
                result = [new AggregatedDocumentDiagnosticSource(result)];
            }

            return result;
        }

        public Project GetProject() => sources[0].GetProject();
        public ProjectOrDocumentId GetId() => sources[0].GetId();
        public TextDocumentIdentifier? GetDocumentIdentifier() => sources[0].GetDocumentIdentifier();
        public string ToDisplayString() => $"{this.GetType().Name}: count={sources.Length}";

        public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var diagnostics);
            foreach (var source in sources)
            {
                var namedDiagnostics = await source.GetDiagnosticsAsync(context, cancellationToken).ConfigureAwait(false);
                diagnostics.AddRange(namedDiagnostics);
            }

            return diagnostics.ToImmutableAndClear();
        }
    }
}
