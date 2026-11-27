// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

/// <summary>
/// Wraps an <see cref="IFindReferencesProgress"/> into an <see cref="IStreamingFindReferencesProgress"/>
/// so it can be used from the new streaming find references APIs.
/// </summary>
internal sealed class StreamingFindReferencesProgressAdapter : IStreamingFindReferencesProgress
{
    private readonly IFindReferencesProgress _progress;

    public IStreamingProgressTracker ProgressTracker { get; }

    public StreamingFindReferencesProgressAdapter(IFindReferencesProgress progress)
    {
        _progress = progress;
        ProgressTracker = new StreamingProgressTracker(async (current, max, ct) =>
        {
            _progress.ReportProgress(current, max);
        });
    }

    public async ValueTask OnCompletedAsync(CancellationToken cancellationToken)
    {
        _progress.OnCompleted();
    }

    public async ValueTask OnDefinitionFoundAsync(SymbolGroup group, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var symbol in group.Symbols)
                _progress.OnDefinitionFound(symbol);
        }
        catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    public async ValueTask OnReferencesFoundAsync(ImmutableArray<(SymbolGroup group, ISymbol symbol, ReferenceLocation location)> references, CancellationToken cancellationToken)
    {
        foreach (var (_, symbol, location) in references)
            _progress.OnReferenceFound(symbol, location);
    }

    public async ValueTask OnStartedAsync(CancellationToken cancellationToken)
    {
        _progress.OnStarted();
    }
}
