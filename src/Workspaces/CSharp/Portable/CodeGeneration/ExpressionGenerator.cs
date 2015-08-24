// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class ExpressionGenerator
    {
        internal static ExpressionSyntax GenerateExpression(
            TypedConstant typedConstant)
        {
            switch (typedConstant.Kind)
            {
                case TypedConstantKind.Primitive:
                case TypedConstantKind.Enum:
                    return GenerateExpression(typedConstant.Type, typedConstant.Value, canUseFieldReference: true);

                case TypedConstantKind.Type:
                    return typedConstant.Value is ITypeSymbol
                        ? SyntaxFactory.TypeOfExpression(((ITypeSymbol)typedConstant.Value).GenerateTypeSyntax())
                        : GenerateNullLiteral();

                case TypedConstantKind.Array:
                    return typedConstant.IsNull ?
                        GenerateNullLiteral() :
                        SyntaxFactory.ImplicitArrayCreationExpression(
                            SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression,
                                SyntaxFactory.SeparatedList(typedConstant.Values.Select(GenerateExpression))));

                default:
                    return GenerateNullLiteral();
            }
        }

        private static ExpressionSyntax GenerateNullLiteral()
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
        }

        internal static ExpressionSyntax GenerateExpression(
            ITypeSymbol type,
            object value,
            bool canUseFieldReference)
        {
            if (value != null)
            {
                if (type.TypeKind == TypeKind.Enum)
                {
                    var enumType = (INamedTypeSymbol)type;
                    return (ExpressionSyntax)CSharpFlagsEnumGenerator.Instance.CreateEnumConstantValue(enumType, value);
                }
                else if (type.IsNullable())
                {
                    // If the type of the argument is T?, then the type of the supplied default value can either be T 
                    // (e.g. int? x = 5) or it can be T? (e.g. SomeStruct? x = null). The below statement handles the case
                    // where the type of the supplied default value is T.
                    return GenerateExpression(((INamedTypeSymbol)type).TypeArguments[0], value, canUseFieldReference);
                }
            }

            return GenerateNonEnumValueExpression(type, value, canUseFieldReference);
        }

        internal static ExpressionSyntax GenerateNonEnumValueExpression(
            ITypeSymbol type, object value, bool canUseFieldReference)
        {
            if (value is bool)
            {
                return SyntaxFactory.LiteralExpression((bool)value
                    ? SyntaxKind.TrueLiteralExpression
                    : SyntaxKind.FalseLiteralExpression);
            }
            else if (value is string)
            {
                var valueString = SymbolDisplay.FormatLiteral((string)value, quote: true);
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(valueString, (string)value));
            }
            else if (value is char)
            {
                var charValue = (char)value;
                var literal = SymbolDisplay.FormatLiteral(charValue, quote: true);
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(literal, charValue));
            }
            else if (value is sbyte)
            {
                return GenerateLiteralExpression(type, (sbyte)value, LiteralSpecialValues.SByteSpecialValues, null, canUseFieldReference, (s, v) => SyntaxFactory.Literal(s, v));
            }
            else if (value is short)
            {
                return GenerateLiteralExpression(type, (short)value, LiteralSpecialValues.Int16SpecialValues, null, canUseFieldReference, (s, v) => SyntaxFactory.Literal(s, v));
            }
            else if (value is int)
            {
                return GenerateLiteralExpression(type, (int)value, LiteralSpecialValues.Int32SpecialValues, null, canUseFieldReference, SyntaxFactory.Literal);
            }
            else if (value is long)
            {
                return GenerateLiteralExpression(type, (long)value, LiteralSpecialValues.Int64SpecialValues, null, canUseFieldReference, SyntaxFactory.Literal);
            }
            else if (value is byte)
            {
                return GenerateLiteralExpression(type, (byte)value, LiteralSpecialValues.ByteSpecialValues, null, canUseFieldReference, (s, v) => SyntaxFactory.Literal(s, v));
            }
            else if (value is ushort)
            {
                return GenerateLiteralExpression(type, (ushort)value, LiteralSpecialValues.UInt16SpecialValues, null, canUseFieldReference, (s, v) => SyntaxFactory.Literal(s, (uint)v));
            }
            else if (value is uint)
            {
                return GenerateLiteralExpression(type, (uint)value, LiteralSpecialValues.UInt32SpecialValues, null, canUseFieldReference, SyntaxFactory.Literal);
            }
            else if (value is ulong)
            {
                return GenerateLiteralExpression(type, (ulong)value, LiteralSpecialValues.UInt64SpecialValues, null, canUseFieldReference, SyntaxFactory.Literal);
            }
            else if (value is float)
            {
                return GenerateSingleLiteralExpression(type, (float)value, canUseFieldReference);
            }
            else if (value is double)
            {
                return GenerateDoubleLiteralExpression(type, (double)value, canUseFieldReference);
            }
            else if (value is decimal)
            {
                return GenerateLiteralExpression(type, (decimal)value, LiteralSpecialValues.DecimalSpecialValues, null, canUseFieldReference, SyntaxFactory.Literal);
            }
            else if (type == null || type.IsReferenceType || type.IsPointerType())
            {
                return GenerateNullLiteral();
            }
            else
            {
                return SyntaxFactory.DefaultExpression(type.GenerateTypeSyntax());
            }
        }

        private static string DetermineSuffix(ITypeSymbol type, object value)
        {
            if (value is float)
            {
                var f = (float)value;
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

            if (value is decimal)
            {
                var d = (decimal)value;
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

        private static ExpressionSyntax GenerateDoubleLiteralExpression(ITypeSymbol type, double value, bool canUseFieldReference)
        {
            if (!canUseFieldReference)
            {
                if (double.IsNaN(value))
                {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression,
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

            return GenerateLiteralExpression(type, value, LiteralSpecialValues.DoubleSpecialValues, "R", canUseFieldReference, SyntaxFactory.Literal);
        }

        private static ExpressionSyntax GenerateSingleLiteralExpression(ITypeSymbol type, float value, bool canUseFieldReference)
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

            return GenerateLiteralExpression(type, value, LiteralSpecialValues.SingleSpecialValues, "R", canUseFieldReference, SyntaxFactory.Literal);
        }

        private static ExpressionSyntax GenerateLiteralExpression<T>(
            ITypeSymbol type, T value, IEnumerable<KeyValuePair<T, string>> constants, string formatString, bool canUseFieldReference, Func<string, T, SyntaxToken> tokenFactory)
        {
            if (canUseFieldReference)
            {
                var result = GenerateFieldReference(type, value, constants);
                if (result != null)
                {
                    return result;
                }
            }

            var suffix = DetermineSuffix(type, value);
            var stringValue = ((IFormattable)value).ToString(formatString, CultureInfo.InvariantCulture) + suffix;
            return SyntaxFactory.LiteralExpression(
               SyntaxKind.NumericLiteralExpression, tokenFactory(stringValue, value));
        }

        private static ExpressionSyntax GenerateFieldReference<T>(ITypeSymbol type, T value, IEnumerable<KeyValuePair<T, string>> constants)
        {
            foreach (var constant in constants)
            {
                if (constant.Key.Equals(value))
                {
                    var memberAccess = GenerateMemberAccess("System", typeof(T).Name);
                    if (type != null && !(type is IErrorTypeSymbol))
                    {
                        memberAccess = memberAccess.WithAdditionalAnnotations(SpecialTypeAnnotation.Create(type.SpecialType));
                    }

                    var result = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, memberAccess, SyntaxFactory.IdentifierName(constant.Value));
                    return result.WithAdditionalAnnotations(Simplifier.Annotation);
                }
            }

            return null;
        }

        private static ExpressionSyntax GenerateMemberAccess(params string[] names)
        {
            ExpressionSyntax result = SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword));
            for (int i = 0; i < names.Length; i++)
            {
                var name = SyntaxFactory.IdentifierName(names[i]);
                if (i == 0)
                {
                    result = SyntaxFactory.AliasQualifiedName((IdentifierNameSyntax)result, name);
                }
                else
                {
                    result = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, result, name);
                }
            }

            result = result.WithAdditionalAnnotations(Simplifier.Annotation);
            return result;
        }
    }
}
