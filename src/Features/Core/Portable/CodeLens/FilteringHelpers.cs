// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.CodeLens
{
    internal sealed class FilteringHelpers
    {
        /// <summary>
        /// Exclude the following kind of symbols:
        ///  1. Implicitly declared symbols (such as implicit fields backing properties)
        ///  2. Symbols that can't be referenced by name (such as property getters and setters).
        ///  3. Metadata only symbols, i.e. symbols with no location in source.
        /// </summary>
        /// <remarks>
        /// This is consumed by the streaming find refs progress counting implementation which does not
        /// store the intermediate results from find refs.
        /// </remarks>
        public static bool FilterReference(ISymbol queriedSymbol, ISymbol definition, ReferenceLocation reference)
        {
            return FilterImplicitDefinition(definition) &&
                   FilterImplicitReference(queriedSymbol, definition) &&
                   (definition.Locations.Any(loc => loc.IsInSource)
                    || reference.Location.IsInSource);
        }

        /// <remarks>
        /// This is consumed by the native find refs count implementation that does a full find refs search
        /// and counts the number of locations. This does not need to filter implicit references as it eventually strips
        /// out all duplicate locations.
        /// </remarks>
        public static bool FilterReference(ISymbol definition, ReferencedSymbol reference)
        {
            return FilterImplicitDefinition(definition) &&
                   (definition.Locations.Any(loc => loc.IsInSource)
                    || reference.Locations.Any(loc => loc.Location.IsInSource));
        }

        public static bool FilterDeclaration(ISymbol definition)
        {
            return FilterImplicitDefinition(definition) &&
                   definition.Locations.Any(loc => loc.IsInSource);
        }

        private static bool FilterImplicitDefinition(ISymbol symbol)
        {
            return !symbol.IsImplicitlyDeclared && !IsAccessor(symbol);
        }

        /// <remarks>
        /// FindRefs treats a constructor invocation as a reference to the constructor symbol and to the named type symbol that defines it.
        /// While we need to count the cascaded symbol definition from the named type to its constructor, we should not double count the
        /// reference location for the invocation while computing references count for the named type symbol. 
        /// </remarks>
        private static bool FilterImplicitReference(ISymbol queriedSymbol, ISymbol definition)
        {
            return !(queriedSymbol.Kind == SymbolKind.NamedType && (definition as IMethodSymbol)?.MethodKind == MethodKind.Constructor);
        }

        ///<remarks>
        /// This method explicity checks if the given reference symbol is an accessor for the queried symbol.
        /// </remarks>
        private static bool IsAccessor(ISymbol referencedSymbol)
        {
            var methodSymbol = referencedSymbol as IMethodSymbol;
            return methodSymbol?.AssociatedSymbol != null;
        }
    }
}
