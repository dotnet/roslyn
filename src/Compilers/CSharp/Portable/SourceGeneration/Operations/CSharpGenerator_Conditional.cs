// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private SyntaxNode? TryGenerateConditional(IConditionalOperation? operation, SyntaxType type)
        {
            if (operation == null || operation.IsImplicit)
                return null;

            if (type == SyntaxType.Statement)
            {
                var condition = TryGenerateExpression(operation.Condition);
                var whenTrue = TryGenerateStatement(WrapWithBlock(operation.WhenTrue));
                if (condition == null || whenTrue == null)
                    return null;

                var whenFalse = TryGenerateStatement(WrapWithBlock(operation.WhenFalse));
                return IfStatement(
                    condition,
                    whenTrue,
                    whenFalse == null ? null : ElseClause(whenFalse));
            }
            else if (type == SyntaxType.Expression)
            {
                var condition = TryGenerateExpression(WrapWithParenthesized(operation.Condition));
                var whenTrue = TryGenerateExpression(WrapWithParenthesized(operation.WhenTrue));
                var whenFalse = TryGenerateExpression(WrapWithParenthesized(operation.WhenFalse));
                if (condition == null || whenTrue == null || whenFalse == null)
                    return null;

                return ConditionalExpression(condition, whenTrue, whenFalse);
            }

            throw new ArgumentException($"Conditional operation can only be converted using {nameof(TryGenerateStatement)} or {nameof(TryGenerateExpression)}");
        }
    }
}
