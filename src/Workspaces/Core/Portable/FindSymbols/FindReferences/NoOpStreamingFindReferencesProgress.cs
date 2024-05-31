// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

/// <summary>
/// A does-nothing version of the <see cref="IStreamingFindReferencesProgress"/>. Useful for
/// clients that have no need to report progress as they work.
/// </summary>
internal class NoOpStreamingFindReferencesProgress : IStreamingFindReferencesProgress
{
    public static readonly IStreamingFindReferencesProgress Instance =
        new NoOpStreamingFindReferencesProgress();

    public IStreamingProgressTracker ProgressTracker { get; } = new NoOpProgressTracker();

    private NoOpStreamingFindReferencesProgress()
    {
    }

    public ValueTask OnCompletedAsync(CancellationToken cancellationToken) => default;
    public ValueTask OnStartedAsync(CancellationToken cancellationToken) => default;
    public ValueTask OnDefinitionFoundAsync(SymbolGroup group, CancellationToken cancellationToken) => default;
    public ValueTask OnReferencesFoundAsync(ImmutableArray<(SymbolGroup group, ISymbol symbol, ReferenceLocation location)> references, CancellationToken cancellationToken) => default;

    private class NoOpProgressTracker : IStreamingProgressTracker
    {
        public ValueTask AddItemsAsync(int count, CancellationToken cancellationToken) => default;
        public ValueTask ItemsCompletedAsync(int count, CancellationToken cancellationToken) => default;
    }
}
