// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ArrowExpressionClauseSyntax
    {
        public ArrowExpressionClauseSyntax Update(SyntaxToken arrowToken, ExpressionSyntax expression)
        {
            return Update(arrowToken, this.RefKeyword, expression);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ArrowExpressionClauseSyntax ArrowExpressionClause(SyntaxToken arrowToken, ExpressionSyntax expression)
        {
            return ArrowExpressionClause(arrowToken, refKeyword: default(SyntaxToken), expression: expression);
        }
    }
}
