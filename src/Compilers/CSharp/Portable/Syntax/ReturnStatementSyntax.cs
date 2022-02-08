// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ReturnStatementSyntax
    {
        public ReturnStatementSyntax Update(SyntaxToken returnKeyword, ExpressionSyntax? expression, SyntaxToken semicolonToken)
            => Update(AttributeLists, returnKeyword, expression, semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ReturnStatementSyntax ReturnStatement(SyntaxToken returnKeyword, ExpressionSyntax? expression, SyntaxToken semicolonToken)
            => ReturnStatement(attributeLists: default, returnKeyword, expression, semicolonToken);
    }
}
