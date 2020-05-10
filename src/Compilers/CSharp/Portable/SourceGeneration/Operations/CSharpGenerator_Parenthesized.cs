// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private ParenthesizedExpressionSyntax? TryGenerateParenthesizedExpression(IParenthesizedOperation? operation, SyntaxType type)
        {
            if (operation == null)
                return null;

            if (type == SyntaxType.Statement)
                throw new ArgumentException("Parenthesized operation cannot be converted to a statement");

            var expression = TryGenerateExpression(operation.Operand);
            return expression == null
                ? null
                : ParenthesizedExpression(expression);
        }

        private IParenthesizedOperation? WrapWithParenthesized(IOperation? operation)
            => operation == null ? null :
               operation is IParenthesizedOperation parenthesized ? parenthesized :
                CodeGenerator.Parenthesized(operation);
    }
}
