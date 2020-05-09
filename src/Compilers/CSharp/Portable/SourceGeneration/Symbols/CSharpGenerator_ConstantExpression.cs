// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.SourceGeneration;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private static ExpressionSyntax? TryGenerateConstantExpression(
            ITypeSymbol type, bool hasConstantValue, object? constantValue)
        {
            if (!hasConstantValue)
                return null;

            return GenerateConstantExpression(type, constantValue, canUseFieldReference: true);
        }

        private static ExpressionSyntax GenerateConstantExpression(
            ITypeSymbol type,
            object? value,
            bool canUseFieldReference)
        {
            if (value != null)
            {
                /*
                 *if (type.TypeKind == TypeKind.Enum)
                {
                    var enumType = (INamedTypeSymbol)type;
                    return (ExpressionSyntax)CSharpFlagsEnumGenerator.Instance.CreateEnumConstantValue(enumType, value);
                }
                else */
                if (type?.SpecialType == SpecialType.System_Nullable_T)
                {
                    // If the type of the argument is T?, then the type of the supplied default value can either be T 
                    // (e.g. int? x = 5) or it can be T? (e.g. SomeStruct? x = null). The below statement handles the case
                    // where the type of the supplied default value is T.
                    return GenerateConstantExpression(((INamedTypeSymbol)type).TypeArguments[0], value, canUseFieldReference);
                }
            }

            return GenerateNonEnumValueExpression(type, value, canUseFieldReference);
        }

        internal static ExpressionSyntax GenerateNonEnumValueExpression(
            ITypeSymbol? type, object? value, bool canUseFieldReference)
            => value switch
            {
                bool val => GenerateBooleanLiteralExpression(val),
                string val => GenerateStringLiteralExpression(val),
                char val => GenerateCharLiteralExpression(val),
                sbyte val => GenerateLiteralExpression(type, val, LiteralSpecialValues.SByteSpecialValues, "", canUseFieldReference, (s, v) => Literal(s, v), x => x < 0, x => (sbyte)-x, "128"),
                short val => GenerateLiteralExpression(type, val, LiteralSpecialValues.Int16SpecialValues, "", canUseFieldReference, (s, v) => Literal(s, v), x => x < 0, x => (short)-x, "32768"),
                int val => GenerateLiteralExpression(type, val, LiteralSpecialValues.Int32SpecialValues, "", canUseFieldReference, SyntaxFactory.Literal, x => x < 0, x => -x, "2147483648"),
                long val => GenerateLiteralExpression(type, val, LiteralSpecialValues.Int64SpecialValues, "", canUseFieldReference, SyntaxFactory.Literal, x => x < 0, x => -x, "9223372036854775808"),
                byte val => GenerateNonNegativeLiteralExpression(type, val, LiteralSpecialValues.ByteSpecialValues, "", canUseFieldReference, (s, v) => Literal(s, v)),
                ushort val => GenerateNonNegativeLiteralExpression(type, val, LiteralSpecialValues.UInt16SpecialValues, "", canUseFieldReference, (s, v) => Literal(s, (uint)v)),
                uint val => GenerateNonNegativeLiteralExpression(type, val, LiteralSpecialValues.UInt32SpecialValues, "", canUseFieldReference, SyntaxFactory.Literal),
                ulong val => GenerateNonNegativeLiteralExpression(type, val, LiteralSpecialValues.UInt64SpecialValues, "", canUseFieldReference, SyntaxFactory.Literal),
                float val => GenerateSingleLiteralExpression(type, val, canUseFieldReference),
                double val => GenerateDoubleLiteralExpression(type, val, canUseFieldReference),
                decimal val => GenerateLiteralExpression(type, val, LiteralSpecialValues.DecimalSpecialValues, "", canUseFieldReference, SyntaxFactory.Literal, x => x < 0, x => -x, ""),
                _ => type == null || type.IsReferenceType || type.TypeKind == TypeKind.Pointer || type.SpecialType == SpecialType.System_Nullable_T
                    ? GenerateNullLiteral()
                    : GenerateDefaultExpression(type),
            };

        private static ExpressionSyntax GenerateNullLiteral()
            => LiteralExpression(SyntaxKind.NullLiteralExpression);

        private static ExpressionSyntax GenerateDefaultExpression(ITypeSymbol type)
            => DefaultExpression(type.GenerateTypeSyntax());

        private static ExpressionSyntax GenerateBooleanLiteralExpression(bool val)
            => LiteralExpression(val ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);

        private static ExpressionSyntax GenerateStringLiteralExpression(string val)
        {
            var valueString = SymbolDisplay.FormatLiteral(val, quote: true);
            return LiteralExpression(
                SyntaxKind.StringLiteralExpression, Literal(valueString, val));
        }

        private static ExpressionSyntax GenerateCharLiteralExpression(char val)
        {
            var literal = SymbolDisplay.FormatLiteral(val, quote: true);
            return LiteralExpression(
                SyntaxKind.CharacterLiteralExpression, Literal(literal, val));
        }

        public static bool IsSpecialType(ITypeSymbol? type, SpecialType specialType)
            => type != null && type.SpecialType == specialType;

        private static string DetermineSuffix(ITypeSymbol? type, object value)
        {
            if (value is float f)
            {
                var stringValue = ((IFormattable)value).ToString("R", CultureInfo.InvariantCulture);

                var isNotSingle = !IsSpecialType(type, SpecialType.System_Single);
                var containsDoubleCharacter =
                    stringValue.Contains("E") || stringValue.Contains("e") || stringValue.Contains(".") ||
                    stringValue.Contains("+") || stringValue.Contains("-");

                if (isNotSingle || containsDoubleCharacter)
                {
                    return "F";
                }
            }

            if (value is double && !IsSpecialType(type, SpecialType.System_Double))
            {
                return "D";
            }

            if (value is uint && !IsSpecialType(type, SpecialType.System_UInt32))
            {
                return "U";
            }

            if (value is long && !IsSpecialType(type, SpecialType.System_Int64))
            {
                return "L";
            }

            if (value is ulong && !IsSpecialType(type, SpecialType.System_UInt64))
            {
                return "UL";
            }

            if (value is decimal d)
            {
                var scale = d.GetScale();

                var isNotDecimal = !IsSpecialType(type, SpecialType.System_Decimal);
                var isOutOfRange = d < long.MinValue || d > long.MaxValue;
                var scaleIsNotZero = scale != 0;

                if (isNotDecimal || isOutOfRange || scaleIsNotZero)
                {
                    return "M";
                }
            }

            return string.Empty;
        }

        private static ExpressionSyntax GenerateDoubleLiteralExpression(ITypeSymbol? type, double value, bool canUseFieldReference)
        {
            if (!canUseFieldReference)
            {
                if (double.IsNaN(value))
                {
                    return BinaryExpression(SyntaxKind.DivideExpression,
                        GenerateDoubleLiteralExpression(null, 0.0, false),
                        GenerateDoubleLiteralExpression(null, 0.0, false));
                }
                else if (double.IsPositiveInfinity(value))
                {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression,
                        GenerateDoubleLiteralExpression(null, 1.0, false),
                        GenerateDoubleLiteralExpression(null, 0.0, false));
                }
                else if (double.IsNegativeInfinity(value))
                {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression,
                        SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, GenerateDoubleLiteralExpression(null, 1.0, false)),
                        GenerateDoubleLiteralExpression(null, 0.0, false));
                }
            }

            return GenerateLiteralExpression(
                type, value, LiteralSpecialValues.DoubleSpecialValues, "R", canUseFieldReference,
                SyntaxFactory.Literal, x => x < 0, x => -x, "");
        }

        private static ExpressionSyntax GenerateSingleLiteralExpression(ITypeSymbol? type, float value, bool canUseFieldReference)
        {
            if (!canUseFieldReference)
            {
                if (float.IsNaN(value))
                {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression,
                        GenerateSingleLiteralExpression(null, 0.0F, false),
                        GenerateSingleLiteralExpression(null, 0.0F, false));
                }
                else if (float.IsPositiveInfinity(value))
                {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression,
                        GenerateSingleLiteralExpression(null, 1.0F, false),
                        GenerateSingleLiteralExpression(null, 0.0F, false));
                }
                else if (float.IsNegativeInfinity(value))
                {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression,
                        SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, GenerateSingleLiteralExpression(null, 1.0F, false)),
                        GenerateSingleLiteralExpression(null, 0.0F, false));
                }
            }

            return GenerateLiteralExpression(
                type, value, LiteralSpecialValues.SingleSpecialValues, "R", canUseFieldReference,
                SyntaxFactory.Literal, x => x < 0, x => -x, "");
        }

        private static ExpressionSyntax GenerateNonNegativeLiteralExpression<T>(
            ITypeSymbol? type, T value, ImmutableArray<(T, string)> constants,
            string formatString, bool canUseFieldReference,
            Func<string, T, SyntaxToken> tokenFactory)
            where T : IEquatable<T>
        {
            return GenerateLiteralExpression(
                type, value, constants, formatString, canUseFieldReference,
                tokenFactory, isNegative: x => false, negate: t => throw new InvalidOperationException(), "");
        }

        private static ExpressionSyntax GenerateLiteralExpression<T>(
            ITypeSymbol? type, T value, ImmutableArray<(T, string)> constants,
            string formatString, bool canUseFieldReference,
            Func<string, T, SyntaxToken> tokenFactory,
            Func<T, bool> isNegative, Func<T, T> negate,
            string integerMinValueString)
            where T : IEquatable<T>
        {
            if (canUseFieldReference)
            {
                var result = TryGenerateFieldReference(value, constants);
                if (result != null)
                    return result;
            }

            var negative = isNegative(value);

            var nonNegativeValue = negative
                ? negate(value)
                : value;

            var suffix = DetermineSuffix(type, nonNegativeValue);

            var stringValue = negative && nonNegativeValue.Equals(value)
                ? integerMinValueString
                : ((IFormattable)nonNegativeValue).ToString(formatString, CultureInfo.InvariantCulture) + suffix;

            var literal = SyntaxFactory.LiteralExpression(
               SyntaxKind.NumericLiteralExpression, tokenFactory(stringValue, nonNegativeValue));

            return negative
                ? PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, literal)
                : (ExpressionSyntax)literal;
        }

        private static ExpressionSyntax? TryGenerateFieldReference<T>(T value, ImmutableArray<(T, string)> constants)
        {
            foreach (var constant in constants)
            {
                if (Equals(constant.Item1, value))
                {
                    var memberAccess = GenerateMemberAccess("System", typeof(T).Name);
                    var result = MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        memberAccess,
                        SyntaxFactory.IdentifierName(constant.Item2));
                    return result;
                }
            }

            return null;
        }

        private static ExpressionSyntax GenerateMemberAccess(params string[] names)
        {
            ExpressionSyntax result = SyntaxFactory.IdentifierName(Token(SyntaxKind.GlobalKeyword));
            for (var i = 0; i < names.Length; i++)
            {
                var name = IdentifierName(names[i]);
                if (i == 0)
                {
                    result = AliasQualifiedName((IdentifierNameSyntax)result, name);
                }
                else
                {
                    result = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, result, name);
                }
            }

            // result = result.WithAdditionalAnnotations(Simplifier.Annotation);
            return result;
        }

        private static ExpressionSyntax GenerateConstantExpression(
            TypedConstant constant)
        {
            return GenerateConstantExpression(constant.Type, constant.Value, canUseFieldReference: true);
        }
    }
}
