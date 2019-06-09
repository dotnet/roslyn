// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Represents a single result of the call to the synchronous
    /// IFindReferencesService.FindReferences method. Finding the references to a symbol will result
    /// in a set of definitions being returned (containing at least the symbol requested) as well as
    /// any references to those definitions in the source. Multiple definitions may be found due to
    /// how C# and VB allow a symbol to be both a definition and a reference at the same time (for
    /// example, a method which implements an interface method).
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public class ReferencedSymbol
    {
        /// <summary>
        /// The symbol definition that these are references to.
        /// </summary>
        public ISymbol Definition => DefinitionAndProjectId.Symbol;

        internal SymbolAndProjectId DefinitionAndProjectId { get; }

        /// <summary>
        /// The set of reference locations in the solution.
        /// </summary>
        public IEnumerable<ReferenceLocation> Locations { get; }

        internal ReferencedSymbol(
            SymbolAndProjectId definitionAndProjectId,
            IEnumerable<ReferenceLocation> locations)
        {
            this.DefinitionAndProjectId = definitionAndProjectId;
            this.Locations = (locations ?? SpecializedCollections.EmptyEnumerable<ReferenceLocation>()).ToReadOnlyCollection();
        }

        private string GetDebuggerDisplay()
        {
            var count = this.Locations.Count();
            return string.Format("{0}, {1} {2}", this.Definition.Name, count, count == 1 ? "ref" : "refs");
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly ReferencedSymbol _referencedSymbol;

            public TestAccessor(ReferencedSymbol referencedSymbol)
            {
                _referencedSymbol = referencedSymbol;
            }

            internal string GetDebuggerDisplay()
                => _referencedSymbol.GetDebuggerDisplay();
        }
    }
}
