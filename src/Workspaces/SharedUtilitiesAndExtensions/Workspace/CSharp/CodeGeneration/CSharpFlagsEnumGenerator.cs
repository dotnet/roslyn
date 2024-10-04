// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

internal class CSharpFlagsEnumGenerator : AbstractFlagsEnumGenerator
{
    public static readonly CSharpFlagsEnumGenerator Instance = new();

    private CSharpFlagsEnumGenerator()
    {
    }

    protected override SyntaxNode CreateExplicitlyCastedLiteralValue(
        SyntaxGenerator generator,
        INamedTypeSymbol enumType,
        SpecialType underlyingSpecialType,
        object constantValue)
    {
        var expression = ExpressionGenerator.GenerateNonEnumValueExpression(
            generator, enumType.EnumUnderlyingType, constantValue, canUseFieldReference: true);

        var constantValueULong = underlyingSpecialType.ConvertUnderlyingValueToUInt64(constantValue);
        if (constantValueULong == 0)
        {
            // 0 is always convertible to an enum type without needing a cast.
            return expression;
        }

        return generator.CastExpression(enumType, expression);
    }

    protected override bool IsValidName(INamedTypeSymbol enumType, string name)
        => SyntaxFacts.IsValidIdentifier(name);
}
