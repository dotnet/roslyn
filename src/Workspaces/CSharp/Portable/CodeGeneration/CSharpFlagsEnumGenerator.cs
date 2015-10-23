﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal class CSharpFlagsEnumGenerator : AbstractFlagsEnumGenerator
    {
        internal static readonly CSharpFlagsEnumGenerator Instance = new CSharpFlagsEnumGenerator();
        private static readonly SyntaxGenerator s_generatorInstance = new CSharpSyntaxGenerator();

        private CSharpFlagsEnumGenerator()
        {
        }

        protected override SyntaxNode CreateExplicitlyCastedLiteralValue(
            INamedTypeSymbol enumType,
            SpecialType underlyingSpecialType,
            object constantValue)
        {
            var expression = ExpressionGenerator.GenerateNonEnumValueExpression(
                enumType.EnumUnderlyingType, constantValue, canUseFieldReference: true);

            var constantValueULong = EnumUtilities.ConvertEnumUnderlyingTypeToUInt64(constantValue, underlyingSpecialType);
            if (constantValueULong == 0)
            {
                // 0 is always convertible to an enum type without needing a cast.
                return expression;
            }

            var factory = new CSharpSyntaxGenerator();
            return factory.CastExpression(enumType, expression);
        }

        protected override SyntaxGenerator GetSyntaxGenerator()
        {
            return s_generatorInstance;
        }

        protected override bool IsValidName(INamedTypeSymbol enumType, string name)
        {
            return SyntaxFacts.IsValidIdentifier(name);
        }
    }
}
