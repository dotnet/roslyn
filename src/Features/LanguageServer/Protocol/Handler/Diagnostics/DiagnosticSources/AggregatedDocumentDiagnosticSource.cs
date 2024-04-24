// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

/// <summary>
/// Aggregates multiple <see cref="IDiagnosticSource"/>s into a single source.
/// </summary>
/// <param name="sources">Sources to aggregate</param>
/// <remarks>
/// Aggregation is usually needed for clients like VS Code which supports single source per request.
/// </remarks>
internal sealed class AggregatedDocumentDiagnosticSource(ImmutableArray<IDiagnosticSource> sources) : IDiagnosticSource
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
