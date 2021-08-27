﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Collects all the <see cref="ISymbol"/> definitions and <see cref="ReferenceLocation"/> 
    /// references that are reported independently and packages them up into the final list
    /// of <see cref="ReferencedSymbol" />.  This is used by the old non-streaming Find-References
    /// APIs to return all the results at the end of the operation, as opposed to broadcasting
    /// the results as they are found.
    /// </summary>
    internal class StreamingProgressCollector : IStreamingFindReferencesProgress
    {
        private readonly object _gate = new();
        private readonly IStreamingFindReferencesProgress _underlyingProgress;

        private readonly Dictionary<ISymbol, List<ReferenceLocation>> _symbolToLocations =
            new();

        public IStreamingProgressTracker ProgressTracker => _underlyingProgress.ProgressTracker;

        public StreamingProgressCollector()
            : this(NoOpStreamingFindReferencesProgress.Instance)
        {
        }

        public StreamingProgressCollector(
            IStreamingFindReferencesProgress underlyingProgress)
        {
            _underlyingProgress = underlyingProgress;
        }

        public ImmutableArray<ReferencedSymbol> GetReferencedSymbols()
        {
            lock (_gate)
            {
                using var _ = ArrayBuilder<ReferencedSymbol>.GetInstance(out var result);
                foreach (var (symbol, locations) in _symbolToLocations)
                    result.Add(new ReferencedSymbol(symbol, locations.ToImmutableArray()));

                return result.ToImmutable();
            }
        }

        public ValueTask OnStartedAsync(CancellationToken cancellationToken) => _underlyingProgress.OnStartedAsync(cancellationToken);
        public ValueTask OnCompletedAsync(CancellationToken cancellationToken) => _underlyingProgress.OnCompletedAsync(cancellationToken);

        public ValueTask OnFindInDocumentCompletedAsync(Document document, CancellationToken cancellationToken) => _underlyingProgress.OnFindInDocumentCompletedAsync(document, cancellationToken);
        public ValueTask OnFindInDocumentStartedAsync(Document document, CancellationToken cancellationToken) => _underlyingProgress.OnFindInDocumentStartedAsync(document, cancellationToken);

        public ValueTask OnDefinitionFoundAsync(SymbolGroup group, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                foreach (var definition in group.Symbols)
                    _symbolToLocations[definition] = new List<ReferenceLocation>();
            }

            return _underlyingProgress.OnDefinitionFoundAsync(group, cancellationToken);
        }

        public ValueTask OnReferenceFoundAsync(SymbolGroup group, ISymbol definition, ReferenceLocation location, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _symbolToLocations[definition].Add(location);
            }

            return _underlyingProgress.OnReferenceFoundAsync(group, definition, location, cancellationToken);
        }
    }
}
