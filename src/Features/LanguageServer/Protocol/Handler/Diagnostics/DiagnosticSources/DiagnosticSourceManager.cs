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
            Contract.ThrowIfFalse(providersDictionary.TryGetValue(sourceName, out var provider), $"Unrecognized source {sourceName}");
            return await provider.CreateDiagnosticSourcesAsync(context, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // VS Code (and legacy VS ?) pass null sourceName when requesting all sources.
            using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var sourcesBuilder);
            foreach (var kvp in providersDictionary)
            {
                // Exclude task diagnostics from the aggregated sources.
                if (kvp.Key != PullDiagnosticCategories.Task)
                {
                    var namedSources = await kvp.Value.CreateDiagnosticSourcesAsync(context, cancellationToken).ConfigureAwait(false);
                    sourcesBuilder.AddRange(namedSources);
                }
            }

            var sources = sourcesBuilder.ToImmutableAndClear();
            if (!isDocument || sources.Length <= 1)
            {
                return sources;
            }
            else
            {
                // VS Code document handler (and legacy VS ?) expects a single source for document diagnostics.
                // For more details see PublicDocumentPullDiagnosticsHandler.CreateReturn.
                Debug.Assert(sources.All(s => s.IsLiveSource()), "All document sources should be live");
                return [new AggregatedDocumentDiagnosticSource(sources)];
            }
        }
    }

    private class AggregatedDocumentDiagnosticSource(ImmutableArray<IDiagnosticSource> sources) : IDiagnosticSource
    {
        public bool IsLiveSource() => true;
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
