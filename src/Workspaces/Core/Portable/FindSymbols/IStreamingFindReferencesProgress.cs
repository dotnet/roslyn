// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal class SymbolGroup : IEquatable<SymbolGroup>
    {
        /// <summary>
        /// The main symbol of the group (normally the symbol that was searched for).
        /// </summary>
        public ISymbol Symbol { get; }

        /// <summary>
        /// All the symbols in the group.  Will include <see cref="Symbol"/>.
        /// </summary>
        public ImmutableHashSet<ISymbol> Symbols { get; }

        private int _hashCode;

        public SymbolGroup(ISymbol symbol, ImmutableArray<ISymbol> symbols)
        {
            Contract.ThrowIfTrue(symbols.IsDefaultOrEmpty);
            Symbol = symbol;
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
                foreach (var symbol in Symbols)
                    _hashCode += MetadataUnifyingEquivalenceComparer.Instance.GetHashCode(symbol);
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

        ValueTask OnStartedAsync();
        ValueTask OnCompletedAsync();

        ValueTask OnFindInDocumentStartedAsync(Document document);
        ValueTask OnFindInDocumentCompletedAsync(Document document);

        ValueTask OnDefinitionFoundAsync(SymbolGroup group);
        ValueTask OnReferenceFoundAsync(SymbolGroup group, ISymbol symbol, ReferenceLocation location);
    }

    internal interface IStreamingFindLiteralReferencesProgress
    {
        IStreamingProgressTracker ProgressTracker { get; }

        ValueTask OnReferenceFoundAsync(Document document, TextSpan span);
    }
}
