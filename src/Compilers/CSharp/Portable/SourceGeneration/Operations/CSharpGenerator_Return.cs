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
                throw new ArgumentException("Return operation cannot be converted to an expression");

            var expression = TryGenerateExpression(operation.ReturnedValue);
            if (operation.Kind == OperationKind.YieldBreak)
                return YieldStatement(SyntaxKind.YieldBreakStatement, expression);

            if (operation.Kind == OperationKind.YieldReturn)
                return YieldStatement(SyntaxKind.YieldReturnStatement, expression);

            return ReturnStatement(expression);
        }
    }
}
