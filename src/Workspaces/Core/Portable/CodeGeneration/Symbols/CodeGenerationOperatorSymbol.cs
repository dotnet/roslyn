// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationOperatorSymbol : CodeGenerationMethodSymbol
    {
        public CodeGenerationOperatorSymbol(
            INamedTypeSymbol containingType,
            IList<AttributeData> attributes,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            ITypeSymbol returnType,
            CodeGenerationOperatorKind operatorKind,
            IList<IParameterSymbol> parameters,
            IList<AttributeData> returnTypeAttributes) :
            base(containingType,
                 attributes,
                 accessibility,
                 modifiers,
                 returnType: returnType,
                 returnsByRef: false,
                 explicitInterfaceSymbolOpt: null,
                 name: GetMetadataName(operatorKind),
                 typeParameters: SpecializedCollections.EmptyList<ITypeParameterSymbol>(),
                 parameters: parameters,
                 returnTypeAttributes: returnTypeAttributes)
        {
        }

        public override MethodKind MethodKind
        {
            get
            {
                return MethodKind.UserDefinedOperator;
            }
        }

        public static int GetParameterCount(CodeGenerationOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case CodeGenerationOperatorKind.Addition:
                case CodeGenerationOperatorKind.BitwiseAnd:
                case CodeGenerationOperatorKind.BitwiseOr:
                case CodeGenerationOperatorKind.Concatenate:
                case CodeGenerationOperatorKind.Division:
                case CodeGenerationOperatorKind.Equality:
                case CodeGenerationOperatorKind.ExclusiveOr:
                case CodeGenerationOperatorKind.Exponent:
                case CodeGenerationOperatorKind.GreaterThan:
                case CodeGenerationOperatorKind.GreaterThanOrEqual:
                case CodeGenerationOperatorKind.Inequality:
                case CodeGenerationOperatorKind.IntegerDivision:
                case CodeGenerationOperatorKind.LeftShift:
                case CodeGenerationOperatorKind.LessThan:
                case CodeGenerationOperatorKind.LessThanOrEqual:
                case CodeGenerationOperatorKind.Like:
                case CodeGenerationOperatorKind.Modulus:
                case CodeGenerationOperatorKind.Multiplication:
                case CodeGenerationOperatorKind.RightShift:
                case CodeGenerationOperatorKind.Subtraction:
                    return 2;
                case CodeGenerationOperatorKind.Increment:
                case CodeGenerationOperatorKind.Decrement:
                case CodeGenerationOperatorKind.False:
                case CodeGenerationOperatorKind.LogicalNot:
                case CodeGenerationOperatorKind.OnesComplement:
                case CodeGenerationOperatorKind.True:
                case CodeGenerationOperatorKind.UnaryPlus:
                case CodeGenerationOperatorKind.UnaryNegation:
                    return 1;
                default:
                    throw ExceptionUtilities.UnexpectedValue(operatorKind);
            }
        }

        private static string GetMetadataName(CodeGenerationOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case CodeGenerationOperatorKind.Addition: return WellKnownMemberNames.AdditionOperatorName;
                case CodeGenerationOperatorKind.BitwiseAnd: return WellKnownMemberNames.BitwiseAndOperatorName;
                case CodeGenerationOperatorKind.BitwiseOr: return WellKnownMemberNames.BitwiseOrOperatorName;
                case CodeGenerationOperatorKind.Concatenate: return WellKnownMemberNames.ConcatenateOperatorName;
                case CodeGenerationOperatorKind.Decrement: return WellKnownMemberNames.DecrementOperatorName;
                case CodeGenerationOperatorKind.Division: return WellKnownMemberNames.DivisionOperatorName;
                case CodeGenerationOperatorKind.Equality: return WellKnownMemberNames.EqualityOperatorName;
                case CodeGenerationOperatorKind.ExclusiveOr: return WellKnownMemberNames.ExclusiveOrOperatorName;
                case CodeGenerationOperatorKind.Exponent: return WellKnownMemberNames.ExponentOperatorName;
                case CodeGenerationOperatorKind.False: return WellKnownMemberNames.FalseOperatorName;
                case CodeGenerationOperatorKind.GreaterThan: return WellKnownMemberNames.GreaterThanOperatorName;
                case CodeGenerationOperatorKind.GreaterThanOrEqual: return WellKnownMemberNames.GreaterThanOrEqualOperatorName;
                case CodeGenerationOperatorKind.Increment: return WellKnownMemberNames.IncrementOperatorName;
                case CodeGenerationOperatorKind.Inequality: return WellKnownMemberNames.InequalityOperatorName;
                case CodeGenerationOperatorKind.IntegerDivision: return WellKnownMemberNames.IntegerDivisionOperatorName;
                case CodeGenerationOperatorKind.LeftShift: return WellKnownMemberNames.LeftShiftOperatorName;
                case CodeGenerationOperatorKind.LessThan: return WellKnownMemberNames.LessThanOperatorName;
                case CodeGenerationOperatorKind.LessThanOrEqual: return WellKnownMemberNames.LessThanOrEqualOperatorName;
                case CodeGenerationOperatorKind.Like: return WellKnownMemberNames.LikeOperatorName;
                case CodeGenerationOperatorKind.LogicalNot: return WellKnownMemberNames.LogicalNotOperatorName;
                case CodeGenerationOperatorKind.Modulus: return WellKnownMemberNames.ModulusOperatorName;
                case CodeGenerationOperatorKind.Multiplication: return WellKnownMemberNames.MultiplyOperatorName;
                case CodeGenerationOperatorKind.OnesComplement: return WellKnownMemberNames.OnesComplementOperatorName;
                case CodeGenerationOperatorKind.RightShift: return WellKnownMemberNames.RightShiftOperatorName;
                case CodeGenerationOperatorKind.Subtraction: return WellKnownMemberNames.SubtractionOperatorName;
                case CodeGenerationOperatorKind.True: return WellKnownMemberNames.TrueOperatorName;
                case CodeGenerationOperatorKind.UnaryPlus: return WellKnownMemberNames.UnaryPlusOperatorName;
                case CodeGenerationOperatorKind.UnaryNegation: return WellKnownMemberNames.UnaryNegationOperatorName;
                default:
                    throw ExceptionUtilities.UnexpectedValue(operatorKind);
            }
        }
    }
}
