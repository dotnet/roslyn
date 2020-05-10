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
        private SyntaxNode? TryGenerateThrowStatementOrExpression(IThrowOperation? operation, SyntaxType type)
        {
            if (operation == null || operation.IsImplicit)
                return null;

            var expression = TryGenerateExpression(operation.Exception);
            if (type == SyntaxType.Statement)
            {
                return ThrowStatement(expression);
            }
            else if (type == SyntaxType.Expression)
            {
                if (expression == null)
                    throw new ArgumentException($"{nameof(IThrowOperation)}.{nameof(IThrowOperation.Exception)} could not be converted to an {nameof(ExpressionSyntax)}");

                return ThrowExpression(expression);
            }

            throw new ArgumentException($"{nameof(IThrowOperation)} can only be converted using {nameof(TryGenerateStatement)} or {nameof(TryGenerateExpression)}");
        }
    }
}
