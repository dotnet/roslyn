// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal partial class ExpressionGenerator : AbstractCSharpCodeGenerator
    {
        internal new ExpressionSyntax GenerateExpression(
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

        internal new ExpressionSyntax GenerateExpression(
            ITypeSymbol type,
            object value,
            bool canUseFieldReference)
        {
            if ((type.OriginalDefinition.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Nullable_T) &&
                (value != null))
            {
                // If the type of the argument is T?, then the type of the supplied default value can either be T 
                // (e.g. int? x = 5) or it can be T? (e.g. SomeStruct? x = null). The below statement handles the case
                // where the type of the supplied default value is T.
                return GenerateExpression(((INamedTypeSymbol)type).TypeArguments[0], value, canUseFieldReference);
            }

            if (type.TypeKind == TypeKind.Enum && value != null)
            {
                var enumType = (INamedTypeSymbol)type;
                return (ExpressionSyntax)CreateEnumConstantValue(enumType, value);
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

            if (value is string)
            {
                var valueString = CSharp.SymbolDisplay.FormatLiteral((string)value, quote: true);
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(valueString, (string)value));
            }

            if (value is char)
            {
                var charValue = (char)value;
                var literal = CSharp.SymbolDisplay.FormatLiteral(charValue, quote: true);
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(literal, charValue));
            }

            if (value is sbyte || value is short || value is int || value is long ||
                value is byte || value is ushort || value is uint || value is ulong)
            {
                var suffix = DetermineSuffix(type, value);
                return GenerateIntegralLiteralExpression(type, value, suffix, canUseFieldReference);
            }

            if (value is float)
            {
                var suffix = DetermineSuffix(type, value);
                return GenerateSingleLiteralExpression(type, (float)value, suffix, canUseFieldReference);
            }

            if (value is double)
            {
                var suffix = DetermineSuffix(type, value);
                return GenerateDoubleLiteralExpression(type, (double)value, suffix, canUseFieldReference);
            }

            if (value is decimal)
            {
                var suffix = DetermineSuffix(type, value);
                return GenerateDecimalLiteralExpression(type, value, suffix, canUseFieldReference);
            }

            if (type == null || type.IsReferenceType || type.IsPointerType())
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

        private static ExpressionSyntax GenerateDecimalLiteralExpression(
            ITypeSymbol type, object value, string suffix, bool canUseFieldReference)
        {
            // don't use field references for simple values
            decimal m = (decimal)value;
            if (m != decimal.MaxValue && m != decimal.MinValue)
            {
                canUseFieldReference = false;
            }

            if (canUseFieldReference)
            {
                var constants = value.GetType().GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsInitOnly);
                var result = GenerateFieldReference(type, value, constants);

                if (result != null)
                {
                    return result;
                }
            }

            var literal = ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture) + suffix;
            return GenerateLiteralExpression(value, literal);
        }

        private static ExpressionSyntax GenerateIntegralLiteralExpression(
            ITypeSymbol type, object value, string suffix, bool canUseFieldReference)
        {
            // If it's the constant value 0, and the type of the value matches the type we want for 
            // this context, then we can just emit the literal 0 here.  We don't want to emit things 
            // like UInteger.MinValue.
            if (value != null && IntegerUtilities.ToUInt64(value) == 0 && (type == null || TypesMatch(type, value)))
            {
                if (TypesMatch(type, value))
                {
                    return GenerateLiteralExpression(0, "0");
                }
                else if (type == null)
                {
                    canUseFieldReference = false;
                }
            }

            var constants = canUseFieldReference ? value.GetType().GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral) : null;
            return GenerateLiteralExpression(type, value, constants, null, suffix, canUseFieldReference);
        }

        private static ExpressionSyntax GenerateDoubleLiteralExpression(ITypeSymbol type, double value, string suffix, bool canUseFieldReference)
        {
            if (!canUseFieldReference)
            {
                if (double.IsNaN(value))
                {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression,
                        GenerateLiteralExpression(0.0, "0.0"),
                        GenerateLiteralExpression(0.0, "0.0"));
                }
                else if (double.IsPositiveInfinity(value))
                {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression,
                        GenerateLiteralExpression(1.0, "1.0"),
                        GenerateLiteralExpression(0.0, "0.0"));
                }
                else if (double.IsNegativeInfinity(value))
                {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression,
                        SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, GenerateLiteralExpression(1.0, "1.0")),
                        GenerateLiteralExpression(0.0, "0.0"));
                }
            }

            return GenerateFloatLiteralExpression(type, value, suffix, canUseFieldReference);
        }

        private static ExpressionSyntax GenerateSingleLiteralExpression(ITypeSymbol type, float value, string suffix, bool canUseFieldReference)
        {
            if (!canUseFieldReference)
            {
                if (float.IsNaN(value))
                {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression,
                        GenerateLiteralExpression(0.0F, "0.0F"),
                        GenerateLiteralExpression(0.0F, "0.0F"));
                }
                else if (float.IsPositiveInfinity(value))
                {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression,
                        GenerateLiteralExpression(1.0F, "1.0F"),
                        GenerateLiteralExpression(0.0F, "0.0F"));
                }
                else if (float.IsNegativeInfinity(value))
                {
                    return SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression,
                        SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, GenerateLiteralExpression(1.0F, "1.0F")),
                        GenerateLiteralExpression(0.0F, "0.0F"));
                }
            }

            return GenerateFloatLiteralExpression(type, value, suffix, canUseFieldReference);
        }

        private static ExpressionSyntax GenerateFloatLiteralExpression(ITypeSymbol type, object value, string suffix, bool canUseFieldReference)
        {
            var constants = value.GetType().GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral);
            return GenerateLiteralExpression(type, value, constants, "R", suffix, canUseFieldReference);
        }

        private static ExpressionSyntax GenerateLiteralExpression(
            ITypeSymbol type, object value, IEnumerable<FieldInfo> constants, string formatString, string suffix, bool canUseFieldReference)
        {
            if (canUseFieldReference)
            {
                var result = GenerateFieldReference(type, value, constants);
                if (result != null)
                {
                    return result;
                }
            }

            var stringValue = ((IFormattable)value).ToString(formatString, CultureInfo.InvariantCulture) + suffix;

            return GenerateLiteralExpression(value, stringValue);
        }

        private static ExpressionSyntax GenerateLiteralExpression(object value, string stringValue)
        {
            var overload = typeof(SyntaxFactory).GetMethod("Literal", new[] { typeof(string), value.GetType() });
            return SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression, (SyntaxToken)overload.Invoke(null, new[] { stringValue, value }));
        }

        private static ExpressionSyntax GenerateFieldReference(ITypeSymbol type, object value, IEnumerable<FieldInfo> constants)
        {
            foreach (var constant in constants)
            {
                if (constant.GetValue(null).Equals(value))
                {
                    var memberAccess = GenerateMemberAccess("System", constant.DeclaringType.Name);
                    if (type != null && !(type is IErrorTypeSymbol))
                    {
                        memberAccess = memberAccess.WithAdditionalAnnotations(SpecialTypeAnnotation.Create(type.SpecialType));
                    }

                    var result = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, memberAccess, SyntaxFactory.IdentifierName(constant.Name));
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

        private static IEnumerable<FieldInfo> GetConstants<TStructure>()
        {
            return typeof(TStructure).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral);
        }
    }
}