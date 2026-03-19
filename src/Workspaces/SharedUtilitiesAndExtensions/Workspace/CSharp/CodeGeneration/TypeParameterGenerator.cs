
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static class TypeParameterGenerator
{
    public static TypeParameterListSyntax? GenerateTypeParameterList(
        ImmutableArray<ITypeParameterSymbol> typeParameters, CSharpCodeGenerationContextInfo info)
    {
        return typeParameters.Length == 0
            ? null
            : TypeParameterList(
                [.. typeParameters.Select(t => GenerateTypeParameter(t, info))]);
    }

    private static TypeParameterSyntax GenerateTypeParameter(ITypeParameterSymbol symbol, CSharpCodeGenerationContextInfo info)
    {
        var varianceKeyword =
            symbol.Variance == VarianceKind.In ? InKeyword :
            symbol.Variance == VarianceKind.Out ? OutKeyword : default;

        return TypeParameter(
            AttributeGenerator.GenerateAttributeLists(symbol.GetAttributes(), info),
            varianceKeyword,
            symbol.Name.ToIdentifierToken());
    }
}
