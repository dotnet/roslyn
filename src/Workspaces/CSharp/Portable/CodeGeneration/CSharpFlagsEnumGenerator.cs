// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal class CSharpFlagsEnumGenerator : AbstractFlagsEnumGenerator
    {
        internal static readonly CSharpFlagsEnumGenerator Instance = new CSharpFlagsEnumGenerator();
        private static readonly SyntaxGenerator s_generatorInstance = CSharpSyntaxGenerator.Instance;

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

            return CSharpSyntaxGenerator.Instance.CastExpression(enumType, expression);
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
