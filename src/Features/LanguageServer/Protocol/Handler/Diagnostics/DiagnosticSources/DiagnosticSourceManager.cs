// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
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
internal class DiagnosticSourceManager : IDiagnosticSourceManager
{
    private readonly ImmutableDictionary<string, IDiagnosticSourceProvider> _documentProviders;
    private readonly ImmutableDictionary<string, IDiagnosticSourceProvider> _workspaceProviders;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DiagnosticSourceManager([ImportMany] IEnumerable<IDiagnosticSourceProvider> sourceProviders)
    {
        _documentProviders = sourceProviders
                .Where(p => p.IsDocument)
                .ToImmutableDictionary(kvp => kvp.Name, kvp => kvp);

        _workspaceProviders = sourceProviders
                .Where(p => !p.IsDocument)
                .ToImmutableDictionary(kvp => kvp.Name, kvp => kvp);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetSourceNames(bool isDocument)
        => (isDocument ? _documentProviders : _workspaceProviders).Keys;

    /// <inheritdoc />
    public async ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, string? sourceName, bool isDocument, CancellationToken cancellationToken)
    {
        var providersDictionary = isDocument ? _documentProviders : _workspaceProviders;
        if (sourceName != null)
        {
            // VS does not distinguish between document and workspace sources. Thus it can request
            // document diagnostics with workspace source name. We need to handle this case.
            if (providersDictionary.TryGetValue(sourceName, out var provider))
                return await provider.CreateDiagnosticSourcesAsync(context, cancellationToken).ConfigureAwait(false);

            return [];
        }
        else
        {
            // VS Code (and legacy VS ?) pass null sourceName when requesting all sources.
            using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var sourcesBuilder);
            foreach (var kvp in providersDictionary)
            {
                // Exclude Task diagnostics from the aggregated sources.
                if (kvp.Key != PullDiagnosticCategories.Task)
                {
                    var namedSources = await kvp.Value.CreateDiagnosticSourcesAsync(context, cancellationToken).ConfigureAwait(false);
                    sourcesBuilder.AddRange(namedSources);
                }
            }

            var sources = sourcesBuilder.ToImmutableAndClear();
            if (sources.Length <= 1)
            {
                return sources;
            }

            if (isDocument)
            {
                // Group all document sources into a single source.
                Debug.Assert(sources.All(s => s.IsLiveSource()), "All document sources should be live");
                sources = [new AggregatedDocumentDiagnosticSource(sources)];
            }
            else
            {
                // For workspace we need to group sources by document and IsLiveSource
                sources = sources.GroupBy(s => (s.GetId(), s.IsLiveSource()), s => s)
                    .SelectMany(g => AggregatedDocumentDiagnosticSource.AggregateIfNeeded(g))
                    .ToImmutableArray();
            }

            return sources;
        }
    }

    private class AggregatedDocumentDiagnosticSource : IDiagnosticSource
    {
        private readonly ImmutableArray<IDiagnosticSource> _sources;

        public static ImmutableArray<IDiagnosticSource> AggregateIfNeeded(IEnumerable<IDiagnosticSource> sources)
        {
            var result = sources.ToImmutableArray();
            if (result.Length > 1)
            {
                result = [new AggregatedDocumentDiagnosticSource(result)];
            }

            return result;
        }

        public AggregatedDocumentDiagnosticSource(ImmutableArray<IDiagnosticSource> sources)
            => this._sources = sources;

        public bool IsLiveSource() => true;
        public Project GetProject() => _sources[0].GetProject();
        public ProjectOrDocumentId GetId() => _sources[0].GetId();
        public TextDocumentIdentifier? GetDocumentIdentifier() => _sources[0].GetDocumentIdentifier();
        public string ToDisplayString() => $"{this.GetType().Name}: count={_sources.Length}";

        public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var diagnostics);
            foreach (var source in _sources)
            {
                var namedDiagnostics = await source.GetDiagnosticsAsync(context, cancellationToken).ConfigureAwait(false);
                diagnostics.AddRange(namedDiagnostics);
            }

            return diagnostics.ToImmutableAndClear();
        }
    }
}
