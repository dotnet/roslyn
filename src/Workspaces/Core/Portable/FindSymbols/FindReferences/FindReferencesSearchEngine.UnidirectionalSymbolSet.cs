// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed partial class FindReferencesSearchEngine
{
    /// <summary>
    /// Symbol set used when <see cref="FindReferencesSearchOptions.UnidirectionalHierarchyCascade"/> is <see
    /// langword="true"/>.  This symbol set will only cascade in a uniform direction once it walks either up or down
    /// from the initial set of symbols. This is the symbol set used for features like 'Find Refs', where we only
    /// want to return location results for members that could feasible actually end up calling into that member at
    /// runtime.  See the docs of <see cref="FindReferencesSearchOptions.UnidirectionalHierarchyCascade"/> for more
    /// information on this.
    /// </summary>
    private sealed class UnidirectionalSymbolSet(
        FindReferencesSearchEngine engine,
        MetadataUnifyingSymbolHashSet initialSymbols,
        MetadataUnifyingSymbolHashSet upSymbols) : SymbolSet(engine)
    {

        /// <summary>
        /// When we're doing a unidirectional find-references, the initial set of up-symbols can never change.
        /// That's because we have computed the up set entirely up front, and no down symbols can produce new
        /// up-symbols (as going down then up would not be unidirectional).
        /// </summary>
        private readonly ImmutableHashSet<ISymbol> _upSymbols = upSymbols.ToImmutableHashSet(MetadataUnifyingEquivalenceComparer.Instance);

        public override ImmutableArray<ISymbol> GetAllSymbols()
        {
            var result = new MetadataUnifyingSymbolHashSet();
            result.AddRange(_upSymbols);
            result.AddRange(initialSymbols);
            return [.. result];
        }

        public override async Task InheritanceCascadeAsync(Project project, CancellationToken cancellationToken)
        {
            // Start searching using the existing set of symbols found at the start (or anything found below that).
            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var workQueue);
            workQueue.AddRange(initialSymbols);

            var projects = ImmutableHashSet.Create(project);

            // Keep adding symbols downwards in this project as long as we keep finding new symbols.
            while (workQueue.TryPop(out var current))
                await AddDownSymbolsAsync(this.Engine, current, initialSymbols, workQueue, projects, cancellationToken).ConfigureAwait(false);
        }
    }
}
