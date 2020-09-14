// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private StatementSyntax? TryGenerateReturnOrYieldStatement(IReturnOperation? operation, SyntaxType type)
        {
            if (operation == null || operation.IsImplicit)
                return null;

            if (type == SyntaxType.Expression)
                throw new ArgumentException($"{nameof(IReturnOperation)} cannot be converted to a {nameof(ExpressionSyntax)}");

            if (operation.Kind == OperationKind.YieldBreak)
                return YieldStatement(SyntaxKind.YieldBreakStatement);

            var expression = TryGenerateExpression(operation.ReturnedValue);
            if (operation.Kind == OperationKind.YieldReturn)
            {
                if (expression == null)
                    throw new ArgumentException($"{nameof(IReturnOperation)}.{nameof(IReturnOperation.ReturnedValue)} could not be converted to an {nameof(ExpressionSyntax)}");

                return YieldStatement(SyntaxKind.YieldReturnStatement, expression);
            }

            return ReturnStatement(expression);
        }
    }
}
