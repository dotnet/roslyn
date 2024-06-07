// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Roslyn.Utilities;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal abstract class CodeGenerationOperatorSymbol(
    INamedTypeSymbol containingType,
    ImmutableArray<AttributeData> attributes,
    Accessibility accessibility,
    DeclarationModifiers modifiers,
    ITypeSymbol returnType,
    CodeGenerationOperatorKind operatorKind,
    ImmutableArray<IParameterSymbol> parameters,
    ImmutableArray<AttributeData> returnTypeAttributes,
    string documentationCommentXml) : CodeGenerationMethodSymbol(containingType,
         attributes,
         accessibility,
         modifiers,
         returnType,
         refKind: RefKind.None,
         explicitInterfaceImplementations: default,
         GetMetadataName(operatorKind),
         typeParameters: ImmutableArray<ITypeParameterSymbol>.Empty,
         parameters,
         returnTypeAttributes,
         documentationCommentXml)
{
    public override MethodKind MethodKind => MethodKind.UserDefinedOperator;

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
            case CodeGenerationOperatorKind.UnsignedRightShift:
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
        => operatorKind switch
        {
            CodeGenerationOperatorKind.Addition => WellKnownMemberNames.AdditionOperatorName,
            CodeGenerationOperatorKind.BitwiseAnd => WellKnownMemberNames.BitwiseAndOperatorName,
            CodeGenerationOperatorKind.BitwiseOr => WellKnownMemberNames.BitwiseOrOperatorName,
            CodeGenerationOperatorKind.Concatenate => WellKnownMemberNames.ConcatenateOperatorName,
            CodeGenerationOperatorKind.Decrement => WellKnownMemberNames.DecrementOperatorName,
            CodeGenerationOperatorKind.Division => WellKnownMemberNames.DivisionOperatorName,
            CodeGenerationOperatorKind.Equality => WellKnownMemberNames.EqualityOperatorName,
            CodeGenerationOperatorKind.ExclusiveOr => WellKnownMemberNames.ExclusiveOrOperatorName,
            CodeGenerationOperatorKind.Exponent => WellKnownMemberNames.ExponentOperatorName,
            CodeGenerationOperatorKind.False => WellKnownMemberNames.FalseOperatorName,
            CodeGenerationOperatorKind.GreaterThan => WellKnownMemberNames.GreaterThanOperatorName,
            CodeGenerationOperatorKind.GreaterThanOrEqual => WellKnownMemberNames.GreaterThanOrEqualOperatorName,
            CodeGenerationOperatorKind.Increment => WellKnownMemberNames.IncrementOperatorName,
            CodeGenerationOperatorKind.Inequality => WellKnownMemberNames.InequalityOperatorName,
            CodeGenerationOperatorKind.IntegerDivision => WellKnownMemberNames.IntegerDivisionOperatorName,
            CodeGenerationOperatorKind.LeftShift => WellKnownMemberNames.LeftShiftOperatorName,
            CodeGenerationOperatorKind.LessThan => WellKnownMemberNames.LessThanOperatorName,
            CodeGenerationOperatorKind.LessThanOrEqual => WellKnownMemberNames.LessThanOrEqualOperatorName,
            CodeGenerationOperatorKind.Like => WellKnownMemberNames.LikeOperatorName,
            CodeGenerationOperatorKind.LogicalNot => WellKnownMemberNames.LogicalNotOperatorName,
            CodeGenerationOperatorKind.Modulus => WellKnownMemberNames.ModulusOperatorName,
            CodeGenerationOperatorKind.Multiplication => WellKnownMemberNames.MultiplyOperatorName,
            CodeGenerationOperatorKind.OnesComplement => WellKnownMemberNames.OnesComplementOperatorName,
            CodeGenerationOperatorKind.RightShift => WellKnownMemberNames.RightShiftOperatorName,
            CodeGenerationOperatorKind.UnsignedRightShift => WellKnownMemberNames.UnsignedRightShiftOperatorName,
            CodeGenerationOperatorKind.Subtraction => WellKnownMemberNames.SubtractionOperatorName,
            CodeGenerationOperatorKind.True => WellKnownMemberNames.TrueOperatorName,
            CodeGenerationOperatorKind.UnaryPlus => WellKnownMemberNames.UnaryPlusOperatorName,
            CodeGenerationOperatorKind.UnaryNegation => WellKnownMemberNames.UnaryNegationOperatorName,
            _ => throw ExceptionUtilities.UnexpectedValue(operatorKind),
        };
}
