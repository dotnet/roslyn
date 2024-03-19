// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery;

/// <summary>
/// Provides a conversion to query.Count().
/// </summary>
internal sealed class ToCountConverter(
    ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo,
    ExpressionSyntax selectExpression,
    ExpressionSyntax modifyingExpression,
    SyntaxTrivia[] trivia) : AbstractToMethodConverter(forEachInfo, selectExpression, modifyingExpression, trivia)
{
    protected override string MethodName => nameof(Enumerable.Count);

    // Checks that the expression is "0".
    protected override bool CanReplaceInitialization(
        ExpressionSyntax expression,
        CancellationToken cancellationToken)
        => expression is LiteralExpressionSyntax literalExpression && literalExpression.Token.ValueText == "0";

    /// Input:
    /// foreach(...)
    /// {
    ///     ...
    ///     ...
    ///     counter++;
    ///  }
    ///  
    ///  Output:
    ///  counter += queryGenerated.Count();
    protected override StatementSyntax CreateDefaultStatement(ExpressionSyntax queryOrLinqInvocationExpression, ExpressionSyntax expression)
        => SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.AddAssignmentExpression,
                expression,
                CreateInvocationExpression(queryOrLinqInvocationExpression)));
}
