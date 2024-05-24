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

internal partial class FindReferencesSearchEngine
{
    /// <summary>
    /// Symbol set used when <see cref="FindReferencesSearchOptions.UnidirectionalHierarchyCascade"/> is <see
    /// langword="false"/>.  This symbol set will cascade up *and* down the inheritance hierarchy for all symbols we
    /// are searching for.  This is the symbol set used for features like 'Rename', where all cascaded symbols must
    /// be updated in order to keep the code compiling.
    /// </summary>
    private sealed class BidirectionalSymbolSet : SymbolSet
    {
        /// <summary>
        /// When we're cascading in both direction, we can just keep all symbols in a single set.  We'll always be
        /// examining all of them to go in both up and down directions in every project we process.  Any time we
        /// add a new symbol to it we'll continue to cascade in both directions looking for more.
        /// </summary>
        private readonly MetadataUnifyingSymbolHashSet _allSymbols = [];
        private readonly bool _includeImplementationsThroughDerivedTypes;

        public BidirectionalSymbolSet(
            FindReferencesSearchEngine engine,
            MetadataUnifyingSymbolHashSet initialSymbols,
            MetadataUnifyingSymbolHashSet upSymbols,
            bool includeImplementationsThroughDerivedTypes)
            : base(engine)
        {
            _allSymbols.AddRange(initialSymbols);
            _allSymbols.AddRange(upSymbols);
            _includeImplementationsThroughDerivedTypes = includeImplementationsThroughDerivedTypes;
        }

        public override ImmutableArray<ISymbol> GetAllSymbols()
            => [.. _allSymbols];

        public override async Task InheritanceCascadeAsync(Project project, CancellationToken cancellationToken)
        {
            // Start searching using the current set of symbols built up so far.
            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var workQueue);
            workQueue.AddRange(_allSymbols);

            var projects = ImmutableHashSet.Create(project);

            while (workQueue.TryPop(out var current))
            {
                // For each symbol we're examining try to walk both up and down from it to see if we discover any
                // new symbols in this project.  As long as we keep finding symbols, we'll keep searching from them
                // in both directions.
                await AddDownSymbolsAsync(this.Engine, current, _allSymbols, workQueue, projects, cancellationToken).ConfigureAwait(false);
                await AddUpSymbolsAsync(this.Engine, current, _allSymbols, workQueue, projects, _includeImplementationsThroughDerivedTypes, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
