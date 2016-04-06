using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PopulateSwitch
{
    internal static class PopulateSwitchHelpers
    {
        public static IReadOnlyList<ISymbol> GetUnusedSwitchLabels<TExpressionSyntax>(
            SemanticModel model,
            INamedTypeSymbol enumType,
            IReadOnlyList<TExpressionSyntax> labelNames) where TExpressionSyntax : SyntaxNode
        {
            var unusedSymbols = new List<ISymbol>();
            foreach (var member in enumType.GetMembers())
            {
                // skip `.ctor` and `__value`
                var fieldSymbol = member as IFieldSymbol;
                if (fieldSymbol == null || fieldSymbol.Type.SpecialType != SpecialType.None)
                {
                    continue;
                }

                unusedSymbols.Add(member);
            }

            foreach (var label in labelNames)
            {
                var symbol = model.GetSymbolInfo(label).Symbol;
                if (symbol == null)
                {
                    // something is wrong with the label and the SemanticModel was unable to 
                    // determine its symbol.  Abort the analyzer by considering this switch
                    // statement as complete.
                    return SpecializedCollections.EmptyReadOnlyList<ISymbol>();
                }

                unusedSymbols.Remove(symbol);
            }

            return unusedSymbols;
        }
    }
}
