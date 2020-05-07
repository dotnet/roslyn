// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static ArrayTypeSyntax GenerateArrayTypeSyntaxWithoutNullable(IArrayTypeSymbol symbol)
        {
            using var _ = GetArrayBuilder<ExpressionSyntax>(out var sizes);

            for (int i = 0; i < symbol.Rank; i++)
                sizes.Add(OmittedArraySizeExpression());

            return ArrayType(
                symbol.ElementType.GenerateTypeSyntax(),
                SingletonList(ArrayRankSpecifier(SeparatedList(sizes))));
        }
    }
}
