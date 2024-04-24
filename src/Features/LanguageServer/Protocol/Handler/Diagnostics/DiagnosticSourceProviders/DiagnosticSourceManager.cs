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

    /// <inheritdoc />
    public ImmutableArray<string> GetSourceNames(bool isDocument)
        => (isDocument ? _nameToDocumentProviderMap : _nameToWorkspaceProviderMap).Keys.ToImmutableArray();

    public ValueTask<ImmutableArray<IDiagnosticSource>> CreateDocumentDiagnosticSourcesAsync(RequestContext context, string? sourceName, CancellationToken cancellationToken)
        => CreateDiagnosticSourcesAsync(context, sourceName, _nameToDocumentProviderMap, isDocument: true, cancellationToken);

    public ValueTask<ImmutableArray<IDiagnosticSource>> CreateWorkspaceDiagnosticSourcesAsync(RequestContext context, string? sourceName, CancellationToken cancellationToken)
        => CreateDiagnosticSourcesAsync(context, sourceName, _nameToWorkspaceProviderMap, isDocument: false, cancellationToken);

    private static async ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(
        RequestContext context,
        string? sourceName,
        ImmutableDictionary<string, IDiagnosticSourceProvider> nameToProviderMap,
        bool isDocument,
        CancellationToken cancellationToken)
    {
        if (sourceName != null)
        {
            // VS does not distinguish between document and workspace sources. Thus it can request
            // document diagnostics with workspace source name. We need to handle this case.
            if (nameToProviderMap.TryGetValue(sourceName, out var provider))
                return await provider.CreateDiagnosticSourcesAsync(context, cancellationToken).ConfigureAwait(false);

            return [];
        }
        else
        {
            // VS Code (and legacy VS ?) pass null sourceName when requesting all sources.
            using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var sourcesBuilder);
            foreach (var (name, provider) in nameToProviderMap)
            {
                // Exclude Task diagnostics from the aggregated sources.
                if (name != PullDiagnosticCategories.Task)
                {
                    var namedSources = await provider.CreateDiagnosticSourcesAsync(context, cancellationToken).ConfigureAwait(false);
                    sourcesBuilder.AddRange(namedSources);
                }
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
            Debug.Assert(sources.All(s => s.IsLiveSource()), "All document sources should be live");
            sources = [new AggregatedDocumentDiagnosticSource(sources)];
        }
        else
        {
            // For workspace we need to group sources by source id and IsLiveSource
            sources = sources.GroupBy(s => (s.GetId(), s.IsLiveSource()), s => s)
                .SelectMany(g => AggregatedDocumentDiagnosticSource.AggregateIfNeeded(g))
                .ToImmutableArray();
        }

        return sources;
    }
}
