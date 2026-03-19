// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression;

using static CSharpSyntaxTokens;

internal static class CSharpUseConditionalExpressionHelpers
{
    public static ExpressionSyntax ConvertToExpression(IThrowOperation throwOperation)
    {
        var throwStatement = (ThrowStatementSyntax)throwOperation.Syntax;
        RoslynDebug.Assert(throwStatement.Expression != null);
        return SyntaxFactory.ThrowExpression(throwStatement.ThrowKeyword, throwStatement.Expression);
    }

    public static (ConditionalExpressionSyntax conditional, bool makeMultiLine) UpdateConditionalExpression(
        IConditionalOperation originalIfStatement,
        ConditionalExpressionSyntax conditional)
    {
        var ifStatement = (IfStatementSyntax)originalIfStatement.Syntax;

        // Move any comments on the `else` keyword to then be on the `:` keyword.  In that case, we definitely want to
        // make this multiline.
        if (ifStatement.Else is null || !ifStatement.Else.ElseKeyword.LeadingTrivia.Any(t => t.IsSingleOrMultiLineComment()))
            return (conditional, makeMultiLine: false);

        var finalConditional = conditional
            .WithColonToken(ColonToken.WithPrependedLeadingTrivia(ifStatement.Else.ElseKeyword.LeadingTrivia))
            .WithWhenTrue(conditional.WhenTrue.WithAppendedTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed));
        return (finalConditional, makeMultiLine: true);
    }
}
