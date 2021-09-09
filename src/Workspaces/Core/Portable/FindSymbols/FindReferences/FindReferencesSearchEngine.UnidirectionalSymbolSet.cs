﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class FindReferencesSearchEngine
    {
        /// <summary>
        /// Symbol set used when <see cref="FindReferencesSearchOptions.UnidirectionalHierarchyCascade"/> is <see
        /// langword="true"/>.  This symbol set will only cascade in a uniform direction once it walks either up or down
        /// from the initial set of symbols. This is the symbol set used for features like 'Find Refs', where we only
        /// want to return location results for members that could feasible actually end up calling into that member at
        /// runtime.  See the docs of <see cref="FindReferencesSearchOptions.UnidirectionalHierarchyCascade"/> for more
        /// information on this.
        /// </summary>
        private sealed class UnidirectionalSymbolSet : SymbolSet
        {
            private readonly MetadataUnifyingSymbolHashSet _initialAndDownSymbols;

            /// <summary>
            /// When we're doing a unidirectional find-references, the initial set of up-symbols can never change.
            /// That's because we have computed the up set entirely up front, and no down symbols can produce new
            /// up-symbols (as going down then up would not be unidirectional).
            /// </summary>
            private readonly ImmutableHashSet<ISymbol> _upSymbols;

            public UnidirectionalSymbolSet(FindReferencesSearchEngine engine, MetadataUnifyingSymbolHashSet initialSymbols, HashSet<ISymbol> upSymbols)
                : base(engine)
            {
                _initialAndDownSymbols = initialSymbols;
                _upSymbols = upSymbols.ToImmutableHashSet();
            }

            public override ImmutableArray<ISymbol> GetAllSymbols()
            {
                using var _ = ArrayBuilder<ISymbol>.GetInstance(_upSymbols.Count + _initialAndDownSymbols.Count, out var result);
                result.AddRange(_upSymbols);
                result.AddRange(_initialAndDownSymbols);
                result.RemoveDuplicates();
                return result.ToImmutable();
            }

            public override async Task InheritanceCascadeAsync(Project project, CancellationToken cancellationToken)
            {
                // Start searching using the existing set of symbols found at the start (or anything found below that).
                var workQueue = new Stack<ISymbol>();
                workQueue.Push(_initialAndDownSymbols);

                var projects = ImmutableHashSet.Create(project);

                while (workQueue.Count > 0)
                {
                    var current = workQueue.Pop();

                    // Keep adding symbols downwards in this project as long as we keep finding new symbols.
                    await AddDownSymbolsAsync(this.Engine, current, _initialAndDownSymbols, workQueue, projects, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
