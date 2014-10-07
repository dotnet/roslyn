using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Rename;

namespace Microsoft.CodeAnalysis.CSharp.Rename
{
    [ExportLanguageService(typeof(IRenamePossibleNameConflictsLanguageService), LanguageNames.CSharp)]
    internal class CSharpRenamePossibleNameConflictsLanguageService : IRenamePossibleNameConflictsLanguageService
    {
        public void TryAddPossibleNameConflicts(ISymbol symbol, string replacementText, List<string> possibleNameConflicts)
        {
            if (replacementText.EndsWith("Attribute") && replacementText.Length > 9)
            {
                var conflict = replacementText.Substring(0, replacementText.Length - 9);
                if (!possibleNameConflicts.Contains(conflict))
                {
                    possibleNameConflicts.Add(conflict);
                }
            }

            if (symbol.Kind == CommonSymbolKind.Property)
            {
                foreach (var conflict in new string[] { "_" + replacementText, "get_" + replacementText, "set_" + replacementText })
                { 
                    if (!possibleNameConflicts.Contains(conflict))
                    {
                        possibleNameConflicts.Add(conflict);
                    }
                }
            }

            // in C# we also need to add the valueText because it can be different from the text in source
            // e.g. it can contain escaped unicode characters. Otherwise conflicts would be detected for
            // v\u0061r and var or similar.
            string valueText = replacementText;
            SyntaxKind kind = SyntaxFacts.GetKeywordKind(replacementText);
            if (kind != SyntaxKind.None)
            {
                valueText = SyntaxFacts.GetText(kind);
            }
            else
            {
                var name = SyntaxFactory.ParseName(replacementText);
                if (name.Kind == SyntaxKind.IdentifierName)
                {
                    valueText = ((Syntax.IdentifierNameSyntax)name).Identifier.ValueText;
                }
            }

            // this also covers the case of an escaped replacementText
            if (valueText != replacementText)
            {
                possibleNameConflicts.Add(valueText);
            }
        }
    }
}
