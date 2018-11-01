using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    internal class PullMemberUpSyntaxRemover
    {
        public void RemoveNode(DocumentEditor editor, SyntaxNode node, ISymbol symbol)
        {
            if (node is VariableDeclaratorSyntax variableDeclarator &&
                (symbol.Kind == SymbolKind.Field || symbol.Kind == SymbolKind.Event))
            {
                if (variableDeclarator.Parent != null &&
                    variableDeclarator.Parent.Parent is BaseFieldDeclarationSyntax fieldOrEventDeclaration)
                {
                    if (fieldOrEventDeclaration.Declaration.Variables.Count() == 1)
                    {
                        // If there is only one variable, e.g.
                        // public int i = 0;
                        // Just remove all
                        editor.RemoveNode(fieldOrEventDeclaration);
                    }
                    else
                    {
                        // If there are multiple variables, e.g.
                        // public int i, j = 0;
                        // Remove only one variable
                        editor.RemoveNode(variableDeclarator);
                    }
                }
            }
            else
            {
                editor.RemoveNode(node);
            }
        }
    }
}
