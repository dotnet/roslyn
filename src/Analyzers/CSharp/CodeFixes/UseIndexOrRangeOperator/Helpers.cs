// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator;

internal static class CodeFixHelpers
{
    /// <summary>
    /// Creates an `^expr` index expression from a given `expr`.
    /// </summary>
    public static PrefixUnaryExpressionSyntax IndexExpression(ExpressionSyntax expr)
        => SyntaxFactory.PrefixUnaryExpression(
            SyntaxKind.IndexExpression,
            expr.Parenthesize());
}
