using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class ISymbolExtensions
    {
        public static bool IsKind(this ISymbol symbol, SymbolKind kind)
        {
            return symbol.MatchesKind(kind);
        }

        public static bool MatchesKind(this ISymbol symbol, SymbolKind kind)
        {
            var csharpSymbol = symbol as Symbol;
            if (csharpSymbol == null)
            {
                return false;
            }

            return csharpSymbol.Kind == kind;
        }

        public static bool MatchesKind(this ISymbol symbol, SymbolKind kind1, SymbolKind kind2)
        {
            var csharpSymbol = symbol as Symbol;
            if (csharpSymbol == null)
            {
                return false;
            }

            return csharpSymbol.Kind == kind1
                || csharpSymbol.Kind == kind2;
        }

        public static bool MatchesKind(this ISymbol symbol, SymbolKind kind1, SymbolKind kind2, SymbolKind kind3)
        {
            var csharpSymbol = symbol as Symbol;
            if (csharpSymbol == null)
            {
                return false;
            }

            return csharpSymbol.Kind == kind1
                || csharpSymbol.Kind == kind2
                || csharpSymbol.Kind == kind3;
        }

        public static bool MatchesKind(this ISymbol symbol, params SymbolKind[] kinds)
        {
            var csharpSymbol = symbol as Symbol;
            if (csharpSymbol == null)
            {
                return false;
            }

            return kinds.Contains(csharpSymbol.Kind);
        }
    }
}
