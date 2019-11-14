using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping.InitializerExpression
{
    internal partial class CSharpInitializerExpression : AbstractCSharpInitializerExpressionWrapper<InitializerExpressionSyntax, ExpressionSyntax>
    {
        protected override SeparatedSyntaxList<ExpressionSyntax> GetListItems(InitializerExpressionSyntax listSyntax)
        {
            return listSyntax.Expressions;
        }

        protected override bool PositionIsApplicable(SyntaxNode root, int position, SyntaxNode declaration, InitializerExpressionSyntax listSyntax)
        {
            var startToken = listSyntax.GetFirstToken();

            var token = root.FindToken(position);
            if (token.Parent.Ancestors().Contains(listSyntax))
            {
                var current = token.Parent;
                while (current != listSyntax)
                {
                    if (CSharpSyntaxFactsService.Instance.IsAnonymousFunction(current))
                    {
                        return false;
                    }

                    current = current.Parent;
                }
            }

            return true;
        }

        protected override InitializerExpressionSyntax TryGetApplicableList(SyntaxNode node)
        {
            return node as InitializerExpressionSyntax;
        }
    }
}
