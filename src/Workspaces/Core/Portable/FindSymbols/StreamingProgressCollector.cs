// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly object _gate = new object();
        private readonly IStreamingFindReferencesProgress _underlyingProgress;

        private readonly Dictionary<SymbolAndProjectId, List<ReferenceLocation>> _symbolToLocations =
            new Dictionary<SymbolAndProjectId, List<ReferenceLocation>>();

        public StreamingProgressCollector(
            IStreamingFindReferencesProgress underlyingProgress)
        {
            _underlyingProgress = underlyingProgress;
        }

        public ImmutableArray<ReferencedSymbol> GetReferencedSymbols()
        {
            lock (_gate)
            {
                var result = ArrayBuilder<ReferencedSymbol>.GetInstance();
                foreach (var kvp in _symbolToLocations)
                {
                    result.Add(new ReferencedSymbol(kvp.Key, kvp.Value.ToList()));
                }

                return result.ToImmutableAndFree();
            }
        }

        public Task OnStartedAsync(CancellationToken cancellationToken) => _underlyingProgress.OnStartedAsync(cancellationToken);
        public Task OnCompletedAsync(CancellationToken cancellationToken) => _underlyingProgress.OnCompletedAsync(cancellationToken);
        public Task ReportProgressAsync(int current, int maximum, CancellationToken cancellationToken) => _underlyingProgress.ReportProgressAsync(current, maximum, cancellationToken);

        public Task OnFindInDocumentCompletedAsync(Document document, CancellationToken cancellationToken) => _underlyingProgress.OnFindInDocumentCompletedAsync(document, cancellationToken);
        public Task OnFindInDocumentStartedAsync(Document document, CancellationToken cancellationToken) => _underlyingProgress.OnFindInDocumentStartedAsync(document, cancellationToken);

        public Task OnDefinitionFoundAsync(SymbolAndProjectId definition, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _symbolToLocations[definition] = new List<ReferenceLocation>();
            }

            return _underlyingProgress.OnDefinitionFoundAsync(definition, cancellationToken);
        }

        public Task OnReferenceFoundAsync(SymbolAndProjectId definition, ReferenceLocation location, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _symbolToLocations[definition].Add(location);
            }

            return _underlyingProgress.OnReferenceFoundAsync(definition, location, cancellationToken);
        }
    }
}