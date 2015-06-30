﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class OperatorFacts
    {
        public static bool DefinitelyHasNoUserDefinedOperators(TypeSymbol type)
        {
            // We can take an early out and not look for user-defined operators.

            switch (type.TypeKind)
            {
                case TypeKind.Struct:
                case TypeKind.Class:
                case TypeKind.TypeParameter:
                    break;
                default:
                    return true;
            }

            // System.Decimal does have user-defined operators but it is treated as 
            // though it were a built-in type.
            switch (type.SpecialType)
            {
                case SpecialType.System_Array:
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_Decimal:
                case SpecialType.System_Delegate:
                case SpecialType.System_Double:
                case SpecialType.System_Enum:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_MulticastDelegate:
                case SpecialType.System_Object:
                case SpecialType.System_SByte:
                case SpecialType.System_Single:
                case SpecialType.System_String:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_ValueType:
                case SpecialType.System_Void:
                    return true;
            }

            return false;
        }

        public static string BinaryOperatorNameFromSyntaxKind(SyntaxKind kind)
        {
            return BinaryOperatorNameFromSyntaxKindIfAny(kind) ??
                WellKnownMemberNames.AdditionOperatorName; // This can occur in the presence of syntax errors.
        }

        internal static string BinaryOperatorNameFromSyntaxKindIfAny(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusToken: return WellKnownMemberNames.AdditionOperatorName;
                case SyntaxKind.MinusToken: return WellKnownMemberNames.SubtractionOperatorName;
                case SyntaxKind.AsteriskToken: return WellKnownMemberNames.MultiplyOperatorName;
                case SyntaxKind.SlashToken: return WellKnownMemberNames.DivisionOperatorName;
                case SyntaxKind.PercentToken: return WellKnownMemberNames.ModulusOperatorName;
                case SyntaxKind.CaretToken: return WellKnownMemberNames.ExclusiveOrOperatorName;
                case SyntaxKind.AmpersandToken: return WellKnownMemberNames.BitwiseAndOperatorName;
                case SyntaxKind.BarToken: return WellKnownMemberNames.BitwiseOrOperatorName;
                case SyntaxKind.EqualsEqualsToken: return WellKnownMemberNames.EqualityOperatorName;
                case SyntaxKind.LessThanToken: return WellKnownMemberNames.LessThanOperatorName;
                case SyntaxKind.LessThanEqualsToken: return WellKnownMemberNames.LessThanOrEqualOperatorName;
                case SyntaxKind.LessThanLessThanToken: return WellKnownMemberNames.LeftShiftOperatorName;
                case SyntaxKind.GreaterThanToken: return WellKnownMemberNames.GreaterThanOperatorName;
                case SyntaxKind.GreaterThanEqualsToken: return WellKnownMemberNames.GreaterThanOrEqualOperatorName;
                case SyntaxKind.GreaterThanGreaterThanToken: return WellKnownMemberNames.RightShiftOperatorName;
                case SyntaxKind.ExclamationEqualsToken: return WellKnownMemberNames.InequalityOperatorName;
                default:
                    return null;
            }
        }

        public static string UnaryOperatorNameFromSyntaxKind(SyntaxKind kind)
        {
            return UnaryOperatorNameFromSyntaxKindIfAny(kind) ??
                WellKnownMemberNames.UnaryPlusOperatorName; // This can occur in the presence of syntax errors.
        }

        internal static string UnaryOperatorNameFromSyntaxKindIfAny(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.PlusToken: return WellKnownMemberNames.UnaryPlusOperatorName;
                case SyntaxKind.MinusToken: return WellKnownMemberNames.UnaryNegationOperatorName;
                case SyntaxKind.TildeToken: return WellKnownMemberNames.OnesComplementOperatorName;
                case SyntaxKind.ExclamationToken: return WellKnownMemberNames.LogicalNotOperatorName;
                case SyntaxKind.PlusPlusToken: return WellKnownMemberNames.IncrementOperatorName;
                case SyntaxKind.MinusMinusToken: return WellKnownMemberNames.DecrementOperatorName;
                case SyntaxKind.TrueKeyword: return WellKnownMemberNames.TrueOperatorName;
                case SyntaxKind.FalseKeyword: return WellKnownMemberNames.FalseOperatorName;
                default:
                    return null;
            }
        }

        public static string OperatorNameFromDeclaration(OperatorDeclarationSyntax declaration)
        {
            return OperatorNameFromDeclaration((Syntax.InternalSyntax.OperatorDeclarationSyntax)(declaration.Green));
        }

        public static string OperatorNameFromDeclaration(Syntax.InternalSyntax.OperatorDeclarationSyntax declaration)
        {
            if (SyntaxFacts.IsBinaryExpressionOperatorToken(declaration.OperatorToken.Kind))
            {
                // Some tokens may be either unary or binary operators (e.g. +, -).
                if (SyntaxFacts.IsPrefixUnaryExpressionOperatorToken(declaration.OperatorToken.Kind) &&
                    declaration.ParameterList.Parameters.Count == 1)
                {
                    return OperatorFacts.UnaryOperatorNameFromSyntaxKind(declaration.OperatorToken.Kind);
                }

                return OperatorFacts.BinaryOperatorNameFromSyntaxKind(declaration.OperatorToken.Kind);
            }
            else if (SyntaxFacts.IsUnaryOperatorDeclarationToken(declaration.OperatorToken.Kind))
            {
                return OperatorFacts.UnaryOperatorNameFromSyntaxKind(declaration.OperatorToken.Kind);
            }
            else
            {
                return WellKnownMemberNames.UnaryPlusOperatorName;
            }
        }

        public static string UnaryOperatorNameFromOperatorKind(UnaryOperatorKind kind)
        {
            switch (kind & UnaryOperatorKind.OpMask)
            {
                case UnaryOperatorKind.UnaryPlus: return WellKnownMemberNames.UnaryPlusOperatorName;
                case UnaryOperatorKind.UnaryMinus: return WellKnownMemberNames.UnaryNegationOperatorName;
                case UnaryOperatorKind.BitwiseComplement: return WellKnownMemberNames.OnesComplementOperatorName;
                case UnaryOperatorKind.LogicalNegation: return WellKnownMemberNames.LogicalNotOperatorName;
                case UnaryOperatorKind.PostfixIncrement:
                case UnaryOperatorKind.PrefixIncrement: return WellKnownMemberNames.IncrementOperatorName;
                case UnaryOperatorKind.PostfixDecrement:
                case UnaryOperatorKind.PrefixDecrement: return WellKnownMemberNames.DecrementOperatorName;
                case UnaryOperatorKind.True: return WellKnownMemberNames.TrueOperatorName;
                case UnaryOperatorKind.False: return WellKnownMemberNames.FalseOperatorName;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind & UnaryOperatorKind.OpMask);
            }
        }

        public static string BinaryOperatorNameFromOperatorKind(BinaryOperatorKind kind)
        {
            switch (kind & BinaryOperatorKind.OpMask)
            {
                case BinaryOperatorKind.Addition: return WellKnownMemberNames.AdditionOperatorName;
                case BinaryOperatorKind.And: return WellKnownMemberNames.BitwiseAndOperatorName;
                case BinaryOperatorKind.Division: return WellKnownMemberNames.DivisionOperatorName;
                case BinaryOperatorKind.Equal: return WellKnownMemberNames.EqualityOperatorName;
                case BinaryOperatorKind.GreaterThan: return WellKnownMemberNames.GreaterThanOperatorName;
                case BinaryOperatorKind.GreaterThanOrEqual: return WellKnownMemberNames.GreaterThanOrEqualOperatorName;
                case BinaryOperatorKind.LeftShift: return WellKnownMemberNames.LeftShiftOperatorName;
                case BinaryOperatorKind.LessThan: return WellKnownMemberNames.LessThanOperatorName;
                case BinaryOperatorKind.LessThanOrEqual: return WellKnownMemberNames.LessThanOrEqualOperatorName;
                case BinaryOperatorKind.Multiplication: return WellKnownMemberNames.MultiplyOperatorName;
                case BinaryOperatorKind.Or: return WellKnownMemberNames.BitwiseOrOperatorName;
                case BinaryOperatorKind.NotEqual: return WellKnownMemberNames.InequalityOperatorName;
                case BinaryOperatorKind.Remainder: return WellKnownMemberNames.ModulusOperatorName;
                case BinaryOperatorKind.RightShift: return WellKnownMemberNames.RightShiftOperatorName;
                case BinaryOperatorKind.Subtraction: return WellKnownMemberNames.SubtractionOperatorName;
                case BinaryOperatorKind.Xor: return WellKnownMemberNames.ExclusiveOrOperatorName;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind & BinaryOperatorKind.OpMask);
            }
        }
    }
}
