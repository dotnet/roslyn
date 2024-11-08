// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

/// <summary>
/// Represents a group of <see cref="ISymbol"/>s that should be treated as a single entity for
/// the purposes of presentation in a Find UI.  For example, when a symbol is defined in a file
/// that is linked into multiple project contexts, there will be several unique symbols created
/// that we search for.  Placing these in a group allows the final consumer to know that these 
/// symbols can be merged together.
/// </summary>
internal sealed class SymbolGroup : IEquatable<SymbolGroup>
{
    /// <summary>
    /// All the symbols in the group.
    /// </summary>
    public ImmutableHashSet<ISymbol> Symbols { get; }

    private int _hashCode;

    public SymbolGroup(ImmutableArray<ISymbol> symbols)
    {
        Contract.ThrowIfTrue(symbols.IsDefaultOrEmpty, "Symbols should be non empty");

        // all symbols in the group should be of the same kind
        Debug.Assert(symbols.All(s => s.Kind == symbols[0].Kind));

        Symbols = ImmutableHashSet.CreateRange(
            MetadataUnifyingEquivalenceComparer.Instance, symbols);
    }

    public override bool Equals(object? obj)
        => obj is SymbolGroup group && Equals(group);

    public bool Equals(SymbolGroup? group)
        => this == group || (group != null && Symbols.SetEquals(group.Symbols));

    public override int GetHashCode()
    {
        if (_hashCode == 0)
        {
            var hashCode = 0;
            foreach (var symbol in Symbols)
                hashCode += MetadataUnifyingEquivalenceComparer.Instance.GetHashCode(symbol);
            _hashCode = hashCode;
        }

        return _hashCode;
    }
}

/// <summary>
/// Reports the progress of the FindReferences operation.  Note: these methods may be called on
/// any thread.
/// </summary>
internal interface IStreamingFindReferencesProgress
{
    IStreamingProgressTracker ProgressTracker { get; }

    ValueTask OnStartedAsync(CancellationToken cancellationToken);
    ValueTask OnCompletedAsync(CancellationToken cancellationToken);

    ValueTask OnDefinitionFoundAsync(SymbolGroup group, CancellationToken cancellationToken);
    ValueTask OnReferencesFoundAsync(ImmutableArray<(SymbolGroup group, ISymbol symbol, ReferenceLocation location)> references, CancellationToken cancellationToken);
}

internal interface IStreamingFindLiteralReferencesProgress
{
    IStreamingProgressTracker ProgressTracker { get; }

    ValueTask OnReferenceFoundAsync(Document document, TextSpan span, CancellationToken cancellationToken);
}
