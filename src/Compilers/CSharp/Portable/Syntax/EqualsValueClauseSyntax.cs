// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class EqualsValueClauseSyntax
    {
        public EqualsValueClauseSyntax Update(SyntaxToken equalsToken, ExpressionSyntax value)
        {
            return Update(equalsToken, this.RefKeyword, value);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static EqualsValueClauseSyntax EqualsValueClause(SyntaxToken equalsToken, ExpressionSyntax value)
        {
            return EqualsValueClause(equalsToken, refKeyword: default(SyntaxToken), value: value);
        }
    }
}
