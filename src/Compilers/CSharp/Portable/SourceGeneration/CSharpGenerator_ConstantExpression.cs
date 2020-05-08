// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private static ExpressionSyntax? GenerateConstantExpression(
            ITypeSymbol type, bool hasConstantValue, object? constantValue)
        {
            if (!hasConstantValue)
                return null;

            // TODO.
            return LiteralExpression(SyntaxKind.NullLiteralExpression);
        }

        private static ExpressionSyntax GenerateConstantExpression(
            TypedConstant constant)
        {
            // TODO.
            return LiteralExpression(SyntaxKind.NullLiteralExpression);
        }
    }
}
