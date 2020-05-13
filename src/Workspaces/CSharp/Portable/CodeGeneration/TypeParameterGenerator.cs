// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class TypeParameterGenerator
    {
        public static TypeParameterListSyntax GenerateTypeParameterList(
            ImmutableArray<ITypeParameterSymbol> typeParameters, CodeGenerationOptions options)
        {
            return typeParameters.Length == 0
                ? null
                : SyntaxFactory.TypeParameterList(
                    SyntaxFactory.SeparatedList(typeParameters.Select(t => GenerateTypeParameter(t, options))));
        }

        private static TypeParameterSyntax GenerateTypeParameter(ITypeParameterSymbol symbol, CodeGenerationOptions options)
        {
            var varianceKeyword =
                symbol.Variance == VarianceKind.In ? SyntaxFactory.Token(SyntaxKind.InKeyword) :
                symbol.Variance == VarianceKind.Out ? SyntaxFactory.Token(SyntaxKind.OutKeyword) : default;

            return SyntaxFactory.TypeParameter(
                AttributeGenerator.GenerateAttributeLists(symbol.GetAttributes(), options),
                varianceKeyword,
                symbol.Name.ToIdentifierToken());
        }
    }
}
