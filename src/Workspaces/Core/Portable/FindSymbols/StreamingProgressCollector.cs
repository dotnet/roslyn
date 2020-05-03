// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;

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

        private readonly Dictionary<ISymbol, List<ReferenceLocation>> _symbolToLocations =
            new Dictionary<ISymbol, List<ReferenceLocation>>();

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
                var result = ArrayBuilder<ReferencedSymbol>.GetInstance();
                foreach (var kvp in _symbolToLocations)
                {
                    result.Add(new ReferencedSymbol(kvp.Key, kvp.Value.ToList()));
                }

                return result.ToImmutableAndFree();
            }
        }

        public Task OnStartedAsync() => _underlyingProgress.OnStartedAsync();
        public Task OnCompletedAsync() => _underlyingProgress.OnCompletedAsync();

        public Task OnFindInDocumentCompletedAsync(Document document) => _underlyingProgress.OnFindInDocumentCompletedAsync(document);
        public Task OnFindInDocumentStartedAsync(Document document) => _underlyingProgress.OnFindInDocumentStartedAsync(document);

        public Task OnDefinitionFoundAsync(ISymbol definition)
        {
            lock (_gate)
            {
                _symbolToLocations[definition] = new List<ReferenceLocation>();
            }

            return _underlyingProgress.OnDefinitionFoundAsync(definition);
        }

        public Task OnReferenceFoundAsync(ISymbol definition, ReferenceLocation location)
        {
            lock (_gate)
            {
                _symbolToLocations[definition].Add(location);
            }

            return _underlyingProgress.OnReferenceFoundAsync(definition, location);
        }
    }
}
