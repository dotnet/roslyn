// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping.InitializerExpression
{
    internal partial class CSharpInitializerExpressionWrapper : AbstractCSharpInitializerExpressionWrapper<InitializerExpressionSyntax, ExpressionSyntax>
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
