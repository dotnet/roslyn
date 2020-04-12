// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression
{
    internal static class CSharpUseConditionalExpressionHelpers
    {
        public static bool IsRef(IReturnOperation? returnOperation)
            => returnOperation?.Syntax is ReturnStatementSyntax statement &&
               statement.Expression is RefExpressionSyntax;

        public static ExpressionSyntax ConvertToExpression(IThrowOperation throwOperation)
        {
            var throwStatement = (ThrowStatementSyntax)throwOperation.Syntax;
            return SyntaxFactory.ThrowExpression(throwStatement.ThrowKeyword, throwStatement.Expression);
        }
    }
}
