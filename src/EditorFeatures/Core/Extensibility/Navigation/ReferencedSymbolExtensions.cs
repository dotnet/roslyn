using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.Editor.Navigation
{
    internal static class ReferencedSymbolExtensions
    {
        public static INavigableItem ConvertToNavigableItem(this ReferencedSymbol symbol)
        {
            throw new NotImplementedException();
        }

        public static ImmutableArray<INavigableItem> ConvertToNavigableItems(this IEnumerable<ReferencedSymbol> symbols)
        {
            throw new NotImplementedException();
        }
    }
}