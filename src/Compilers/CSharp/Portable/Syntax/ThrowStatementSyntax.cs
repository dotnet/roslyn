// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ThrowStatementSyntax
    {
        public ThrowStatementSyntax Update(SyntaxToken throwKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken)
            => Update(AttributeLists, throwKeyword, expression, semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ThrowStatementSyntax ThrowStatement(SyntaxToken throwKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken)
            => ThrowStatement(attributeLists: default, throwKeyword, expression, semicolonToken);
    }
}

