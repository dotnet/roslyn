using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Common.Semantics;
using Microsoft.CodeAnalysis.Common.Symbols;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Common.Symbols
{
    /// <summary>
    /// Defines extension methods for the INamedTypeSymbol interface.
    /// </summary>
    public static class INamedTypeSymbolExtensions
    {
        /// <summary>
        /// Returns static constructors.
        /// </summary>
        public static IEnumerable<IMethodSymbol> GetStaticConstructors(this INamedTypeSymbol symbol)
        {
            return symbol.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == CommonMethodKind.StaticConstructor);
        }

        /// <summary>
        /// Get the constructors.
        /// </summary>
        public static IEnumerable<IMethodSymbol> GetConstructors(this INamedTypeSymbol symbol)
        {
            return symbol.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == CommonMethodKind.Constructor || m.MethodKind == CommonMethodKind.StaticConstructor);
        }
    }
}