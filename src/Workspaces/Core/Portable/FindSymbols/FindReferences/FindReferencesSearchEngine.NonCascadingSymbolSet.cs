﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class FindReferencesSearchEngine
    {
        /// <summary>
        /// A symbol set used when the find refs caller does not want cascading.  This is a trivial impl that basically
        /// just wraps the initial symbol provided and doesn't need to do anything beyond that.
        /// </summary>
        private sealed class NonCascadingSymbolSet : SymbolSet
        {
            private readonly ImmutableArray<ISymbol> _symbols;

            public NonCascadingSymbolSet(FindReferencesSearchEngine engine, ISymbol searchSymbol) : base(engine)
            {
                _symbols = ImmutableArray.Create(searchSymbol);
            }

            public override ImmutableArray<ISymbol> GetAllSymbols()
                => _symbols;

            public override Task InheritanceCascadeAsync(Project project, CancellationToken cancellationToken)
            {
                // Nothing to do here.  We're in a non-cascading scenario, so even as we encounter a new project we
                // don't have to figure out what new symbols may be found.
                return Task.CompletedTask;
            }
        }
    }
}
