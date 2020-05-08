// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static ArrayTypeSyntax GenerateArrayTypeSyntaxWithoutNullable(IArrayTypeSymbol symbol, bool onlyNames)
        {
            if (onlyNames)
                throw new ArgumentException("Array cannot be used in a name-only location.");

            using var _ = GetArrayBuilder<ExpressionSyntax>(out var sizes);

            for (int i = 0; i < symbol.Rank; i++)
                sizes.Add(OmittedArraySizeExpression());

            return ArrayType(
                symbol.ElementType.GenerateTypeSyntax(),
                SingletonList(ArrayRankSpecifier(SeparatedList(sizes))));
        }
    }
}
