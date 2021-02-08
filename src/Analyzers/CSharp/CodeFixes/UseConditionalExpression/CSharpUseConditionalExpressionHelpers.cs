// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression
{
    internal static class CSharpUseConditionalExpressionHelpers
    {
        public static ExpressionSyntax ConvertToExpression(IThrowOperation throwOperation)
        {
            var throwStatement = (ThrowStatementSyntax)throwOperation.Syntax;
            RoslynDebug.Assert(throwStatement.Expression != null);
            return SyntaxFactory.ThrowExpression(throwStatement.ThrowKeyword, throwStatement.Expression);
        }
    }
}
