// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => s_generatorInstance;

        protected override bool IsValidName(INamedTypeSymbol enumType, string name)
            => SyntaxFacts.IsValidIdentifier(name);
    }
}
