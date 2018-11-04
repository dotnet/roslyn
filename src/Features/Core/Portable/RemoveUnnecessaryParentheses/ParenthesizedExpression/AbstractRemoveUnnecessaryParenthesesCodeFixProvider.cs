// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses.ParenthesizedExpression
{
    internal abstract class AbstractRemoveUnnecessaryParenthesesCodeFixProvider<TParenthesizedExpressionSyntax>
        : RemoveUnnecessaryParentheses.AbstractRemoveUnnecessaryParenthesesCodeFixProvider<TParenthesizedExpressionSyntax>
        where TParenthesizedExpressionSyntax : SyntaxNode
    {
        protected AbstractRemoveUnnecessaryParenthesesCodeFixProvider()
            : base(Constants.ParenthesizedExpression)
        {
        }

        protected sealed override SyntaxNode Unparenthesize(TParenthesizedExpressionSyntax current)
            => GetSyntaxFactsService().Unparenthesize(current);
    }
}
