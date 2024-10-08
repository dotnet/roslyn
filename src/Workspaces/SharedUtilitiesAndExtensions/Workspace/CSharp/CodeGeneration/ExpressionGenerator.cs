// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

using static CodeGenerationHelpers;
using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static class ExpressionGenerator
{
    public static ExpressionSyntax GenerateExpression(
        SyntaxGenerator generator, TypedConstant typedConstant)
    {
        switch (typedConstant.Kind)
        {
            case TypedConstantKind.Primitive:
            case TypedConstantKind.Enum:
                return GenerateExpression(generator, typedConstant.Type, typedConstant.Value, canUseFieldReference: true);

            case TypedConstantKind.Type:
                return typedConstant.Value is ITypeSymbol typeSymbol
                    ? TypeOfExpression(typeSymbol.GenerateTypeSyntax())
                    : GenerateNullLiteral();

            case TypedConstantKind.Array:
                return typedConstant.IsNull
                    ? GenerateNullLiteral()
                    : ImplicitArrayCreationExpression(
                        InitializerExpression(SyntaxKind.ArrayInitializerExpression,
                            [.. typedConstant.Values.Select(v => GenerateExpression(generator, v))]));

            default:
                return GenerateNullLiteral();
        }
    }

    private static ExpressionSyntax GenerateNullLiteral()
        => LiteralExpression(SyntaxKind.NullLiteralExpression);

    internal static ExpressionSyntax GenerateExpression(
        SyntaxGenerator generator,
        ITypeSymbol? type,
        object? value,
        bool canUseFieldReference)
    {
        if (type != null && value != null)
        {
            if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
                return (ExpressionSyntax)CSharpFlagsEnumGenerator.Instance.CreateEnumConstantValue(generator, enumType, value);

            if (type.IsNullable(out var underlyingType))
            {
                // If the type of the argument is T?, then the type of the supplied default value can either be T 
                // (e.g. int? x = 5) or it can be T? (e.g. SomeStruct? x = null). The below statement handles the case
                // where the type of the supplied default value is T.
                return GenerateExpression(generator, underlyingType, value, canUseFieldReference);
            }
        }

        return GenerateNonEnumValueExpression(generator, type, value, canUseFieldReference);
    }

    internal static ExpressionSyntax GenerateNonEnumValueExpression(SyntaxGenerator generator, ITypeSymbol? type, object? value, bool canUseFieldReference)
        => value switch
        {
            bool val => GenerateBooleanLiteralExpression(val),
            string val => GenerateStringLiteralExpression(val),
            char val => GenerateCharLiteralExpression(val),
            sbyte val => GenerateLiteralExpression(type, val, LiteralSpecialValues.SByteSpecialValues, formatString: null, canUseFieldReference, (s, v) => Literal(s, v), x => x < 0, x => (sbyte)-x, "128"),
            short val => GenerateLiteralExpression(type, val, LiteralSpecialValues.Int16SpecialValues, formatString: null, canUseFieldReference, (s, v) => Literal(s, v), x => x < 0, x => (short)-x, "32768"),
            int val => GenerateLiteralExpression(type, val, LiteralSpecialValues.Int32SpecialValues, formatString: null, canUseFieldReference, Literal, x => x < 0, x => -x, "2147483648"),
            long val => GenerateLiteralExpression(type, val, LiteralSpecialValues.Int64SpecialValues, formatString: null, canUseFieldReference, Literal, x => x < 0, x => -x, "9223372036854775808"),
            byte val => GenerateNonNegativeLiteralExpression(type, val, LiteralSpecialValues.ByteSpecialValues, formatString: null, canUseFieldReference, (s, v) => Literal(s, v)),
            ushort val => GenerateNonNegativeLiteralExpression(type, val, LiteralSpecialValues.UInt16SpecialValues, formatString: null, canUseFieldReference, (s, v) => Literal(s, (uint)v)),
            uint val => GenerateNonNegativeLiteralExpression(type, val, LiteralSpecialValues.UInt32SpecialValues, formatString: null, canUseFieldReference, Literal),
            ulong val => GenerateNonNegativeLiteralExpression(type, val, LiteralSpecialValues.UInt64SpecialValues, formatString: null, canUseFieldReference, Literal),
            float val => GenerateSingleLiteralExpression(type, val, canUseFieldReference),
            double val => GenerateDoubleLiteralExpression(type, val, canUseFieldReference),
            decimal val => GenerateLiteralExpression(type, val, LiteralSpecialValues.DecimalSpecialValues, formatString: null, canUseFieldReference, Literal, x => x < 0, x => -x, integerMinValueString: null),
            _ => type == null || type.IsReferenceType || type is IPointerTypeSymbol || type.IsNullable()
                ? GenerateNullLiteral()
                : (ExpressionSyntax)generator.DefaultExpression(type),
        };

    private static ExpressionSyntax GenerateBooleanLiteralExpression(bool val)
    {
        return LiteralExpression(val
            ? SyntaxKind.TrueLiteralExpression
            : SyntaxKind.FalseLiteralExpression);
    }

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

    private static string DetermineSuffix(ITypeSymbol? type, object value)
    {
        if (value is float)
        {
            var stringValue = ((IFormattable)value).ToString("R", CultureInfo.InvariantCulture);

            var isNotSingle = !IsSpecialType(type, SpecialType.System_Single);
            var containsDoubleCharacter =
                stringValue.Contains('E') || stringValue.Contains('e') || stringValue.Contains('.') ||
                stringValue.Contains('+') || stringValue.Contains('-');

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
            var isOutOfRange = d is < long.MinValue or > long.MaxValue;
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
                    GenerateDoubleLiteralExpression(type: null, 0.0, canUseFieldReference: false),
                    GenerateDoubleLiteralExpression(type: null, 0.0, canUseFieldReference: false));
            }
            else if (double.IsPositiveInfinity(value))
            {
                return BinaryExpression(SyntaxKind.DivideExpression,
                    GenerateDoubleLiteralExpression(type: null, 1.0, canUseFieldReference: false),
                    GenerateDoubleLiteralExpression(type: null, 0.0, canUseFieldReference: false));
            }
            else if (double.IsNegativeInfinity(value))
            {
                return BinaryExpression(SyntaxKind.DivideExpression,
                    PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, GenerateDoubleLiteralExpression(null, 1.0, false)),
                    GenerateDoubleLiteralExpression(type: null, 0.0, canUseFieldReference: false));
            }
        }

        return GenerateLiteralExpression(
            type, value, LiteralSpecialValues.DoubleSpecialValues, formatString: "R", canUseFieldReference,
            Literal, x => x < 0, x => -x, integerMinValueString: null);
    }

    private static ExpressionSyntax GenerateSingleLiteralExpression(ITypeSymbol? type, float value, bool canUseFieldReference)
    {
        if (!canUseFieldReference)
        {
            if (float.IsNaN(value))
            {
                return BinaryExpression(SyntaxKind.DivideExpression,
                    GenerateSingleLiteralExpression(type: null, 0.0F, canUseFieldReference: false),
                    GenerateSingleLiteralExpression(type: null, 0.0F, canUseFieldReference: false));
            }
            else if (float.IsPositiveInfinity(value))
            {
                return BinaryExpression(SyntaxKind.DivideExpression,
                    GenerateSingleLiteralExpression(type: null, 1.0F, canUseFieldReference: false),
                    GenerateSingleLiteralExpression(type: null, 0.0F, canUseFieldReference: false));
            }
            else if (float.IsNegativeInfinity(value))
            {
                return BinaryExpression(SyntaxKind.DivideExpression,
                    PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, GenerateSingleLiteralExpression(null, 1.0F, false)),
                    GenerateSingleLiteralExpression(null, 0.0F, false));
            }
        }

        return GenerateLiteralExpression(
            type, value, LiteralSpecialValues.SingleSpecialValues, formatString: "R", canUseFieldReference,
            Literal, x => x < 0, x => -x, null);
    }

    private static ExpressionSyntax GenerateNonNegativeLiteralExpression<T>(
        ITypeSymbol? type, T value, IEnumerable<KeyValuePair<T, string>> constants,
        string? formatString, bool canUseFieldReference,
        Func<string, T, SyntaxToken> tokenFactory)
        where T : IEquatable<T>
    {
        return GenerateLiteralExpression(
            type, value, constants, formatString, canUseFieldReference,
            tokenFactory, isNegative: x => false, negate: t => throw new InvalidOperationException(), integerMinValueString: null);
    }

    private static ExpressionSyntax GenerateLiteralExpression<T>(
        ITypeSymbol? type, T value, IEnumerable<KeyValuePair<T, string>> constants,
        string? formatString, bool canUseFieldReference,
        Func<string, T, SyntaxToken> tokenFactory,
        Func<T, bool> isNegative, Func<T, T> negate,
        string? integerMinValueString)
        where T : IEquatable<T>
    {
        if (canUseFieldReference)
        {
            var result = GenerateFieldReference(type, value, constants);
            if (result != null)
            {
                return result;
            }
        }

        var negative = isNegative(value);

        var nonNegativeValue = negative
            ? negate(value)
            : value;

        var suffix = DetermineSuffix(type, nonNegativeValue);

        var stringValue = negative && nonNegativeValue.Equals(value)
            ? (integerMinValueString ?? throw ExceptionUtilities.Unreachable())
            : ((IFormattable)nonNegativeValue).ToString(formatString, CultureInfo.InvariantCulture) + suffix;

        var literal = LiteralExpression(
           SyntaxKind.NumericLiteralExpression, tokenFactory(stringValue, nonNegativeValue));

        return negative
            ? PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, literal)
            : literal;
    }

    private static ExpressionSyntax? GenerateFieldReference<T>(ITypeSymbol? type, T value, IEnumerable<KeyValuePair<T, string>> constants)
        where T : IEquatable<T>
    {
        foreach (var constant in constants)
        {
            if (constant.Key.Equals(value))
            {
                var memberAccess = GenerateMemberAccess("System", typeof(T).Name);
                if (type is not null and not IErrorTypeSymbol)
                {
                    memberAccess = memberAccess.WithAdditionalAnnotations(SpecialTypeAnnotation.Create(type.SpecialType));
                }

                var result = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, memberAccess, IdentifierName(constant.Value));
                return result.WithAdditionalAnnotations(Simplifier.Annotation);
            }
        }

        return null;
    }

    private static ExpressionSyntax GenerateMemberAccess(params string[] names)
    {
        ExpressionSyntax result = IdentifierName(GlobalKeyword);
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

        result = result.WithAdditionalAnnotations(Simplifier.Annotation);
        return result;
    }
}
