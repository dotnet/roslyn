// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class YieldStatementSyntax
    {
        public YieldStatementSyntax Update(SyntaxToken yieldKeyword, SyntaxToken returnOrBreakKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken)
            => Update(AttributeLists, yieldKeyword, returnOrBreakKeyword, expression, semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static YieldStatementSyntax YieldStatement(SyntaxKind kind, SyntaxToken yieldKeyword, SyntaxToken returnOrBreakKeyword, ExpressionSyntax expression, SyntaxToken semicolonToken)
            => YieldStatement(kind, attributeLists: default, yieldKeyword, returnOrBreakKeyword, expression, semicolonToken);
    }
}
