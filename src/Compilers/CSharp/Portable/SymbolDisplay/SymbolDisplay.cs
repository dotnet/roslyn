// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
#pragma warning disable CA1200 // Avoid using cref tags with a prefix
    /// <summary>
    /// Displays a symbol in the C# style.
    /// </summary>
    /// <seealso cref="T:Microsoft.CodeAnalysis.VisualBasic.SymbolDisplay"/>
#pragma warning restore CA1200 // Avoid using cref tags with a prefix
    public static class SymbolDisplay
    {
        /// <summary>
        /// Displays a symbol in the C# style, based on a <see cref="SymbolDisplayFormat"/>.
        /// </summary>
        /// <param name="symbol">The symbol to be displayed.</param>
        /// <param name="format">The formatting options to apply.  If null is passed, <see cref="SymbolDisplayFormat.CSharpErrorMessageFormat"/> will be used.</param>
        /// <returns>A formatted string that can be displayed to the user.</returns>
        /// <remarks>
        /// The return value is not expected to be syntactically valid C#.
        /// </remarks>
        public static string ToDisplayString(
            ISymbol symbol,
            SymbolDisplayFormat? format = null)
        {
            return ToDisplayParts(symbol, format).ToDisplayString();
        }

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static string ToDisplayString(
            ITypeSymbol symbol,
            CodeAnalysis.NullableFlowState nullableFlowState,
            SymbolDisplayFormat? format = null)
        {
            return ToDisplayParts(symbol, nullableFlowState, format).ToDisplayString();
        }

        public static string ToDisplayString(
            ITypeSymbol symbol,
            CodeAnalysis.NullableAnnotation nullableAnnotation,
            SymbolDisplayFormat? format = null)
        {
            return ToDisplayParts(symbol, nullableAnnotation, format).ToDisplayString();
        }
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

        /// <summary>
        /// Displays a symbol in the C# style, based on a <see cref="SymbolDisplayFormat"/>.
        /// Based on the context, qualify type and member names as little as possible without
        /// introducing ambiguities.
        /// </summary>
        /// <param name="symbol">The symbol to be displayed.</param>
        /// <param name="semanticModel">Semantic information about the context in which the symbol is being displayed.</param>
        /// <param name="position">A position within the <see cref="SyntaxTree"/> or <paramref name="semanticModel"/>.</param>
        /// <param name="format">The formatting options to apply.  If null is passed, <see cref="SymbolDisplayFormat.CSharpErrorMessageFormat"/> will be used.</param>
        /// <returns>A formatted string that can be displayed to the user.</returns>
        /// <remarks>
        /// The return value is not expected to be syntactically valid C#.
        /// </remarks>
        public static string ToMinimalDisplayString(
            ISymbol symbol,
            SemanticModel semanticModel,
            int position,
            SymbolDisplayFormat? format = null)
        {
            return ToMinimalDisplayParts(symbol, semanticModel, position, format).ToDisplayString();
        }

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static string ToMinimalDisplayString(
            ITypeSymbol symbol,
            CodeAnalysis.NullableFlowState nullableFlowState,
            SemanticModel semanticModel,
            int position,
            SymbolDisplayFormat? format = null)
        {
            return ToMinimalDisplayParts(symbol, nullableFlowState, semanticModel, position, format).ToDisplayString();
        }

        public static string ToMinimalDisplayString(
            ITypeSymbol symbol,
            CodeAnalysis.NullableAnnotation nullableAnnotation,
            SemanticModel semanticModel,
            int position,
            SymbolDisplayFormat? format = null)
        {
            return ToMinimalDisplayParts(symbol, nullableAnnotation, semanticModel, position, format).ToDisplayString();
        }
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

        /// <summary>
        /// Convert a symbol to an array of string parts, each of which has a kind. Useful for
        /// colorizing the display string.
        /// </summary>
        /// <param name="symbol">The symbol to be displayed.</param>
        /// <param name="format">The formatting options to apply.  If null is passed, <see cref="SymbolDisplayFormat.CSharpErrorMessageFormat"/> will be used.</param>
        /// <returns>A list of display parts.</returns>
        /// <remarks>
        /// Parts are not localized until they are converted to strings.
        /// </remarks>
        public static ImmutableArray<SymbolDisplayPart> ToDisplayParts(
            ISymbol symbol,
            SymbolDisplayFormat? format = null)
        {
            // null indicates the default format
            format = format ?? SymbolDisplayFormat.CSharpErrorMessageFormat;
            return ToDisplayParts(
                symbol, semanticModelOpt: null, positionOpt: -1, format: format, minimal: false);
        }

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        // https://github.com/dotnet/roslyn/issues/35035: Add tests
        public static ImmutableArray<SymbolDisplayPart> ToDisplayParts(
            ITypeSymbol symbol,
            CodeAnalysis.NullableFlowState nullableFlowState,
            SymbolDisplayFormat? format = null)
        {
            // null indicates the default format
            format = format ?? SymbolDisplayFormat.CSharpErrorMessageFormat;
            return ToDisplayParts(
                symbol, nullableFlowState, semanticModelOpt: null, positionOpt: -1, format: format, minimal: false);
        }

        public static ImmutableArray<SymbolDisplayPart> ToDisplayParts(
            ITypeSymbol symbol,
            CodeAnalysis.NullableAnnotation nullableAnnotation,
            SymbolDisplayFormat? format = null)
        {
            // null indicates the default format
            format ??= SymbolDisplayFormat.CSharpErrorMessageFormat;
            return ToDisplayParts(
                symbol.WithNullableAnnotation(nullableAnnotation), semanticModelOpt: null, positionOpt: -1, format: format, minimal: false);
        }
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

        /// <summary>
        /// Convert a symbol to an array of string parts, each of which has a kind. Useful for
        /// colorizing the display string.
        /// </summary>
        /// <param name="symbol">The symbol to be displayed.</param>
        /// <param name="semanticModel">Semantic information about the context in which the symbol is being displayed.</param>
        /// <param name="position">A position within the <see cref="SyntaxTree"/> or <paramref name="semanticModel"/>.</param>
        /// <param name="format">The formatting options to apply.  If null is passed, <see cref="SymbolDisplayFormat.CSharpErrorMessageFormat"/> will be used.</param>
        /// <returns>A list of display parts.</returns>
        /// <remarks>
        /// Parts are not localized until they are converted to strings.
        /// </remarks>
        public static ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(
            ISymbol symbol,
            SemanticModel semanticModel,
            int position,
            SymbolDisplayFormat? format = null)
        {
            format ??= SymbolDisplayFormat.MinimallyQualifiedFormat;
            return ToDisplayParts(symbol, semanticModel, position, format, minimal: true);
        }

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        // https://github.com/dotnet/roslyn/issues/35035: Add tests
        public static ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(
            ITypeSymbol symbol,
            CodeAnalysis.NullableFlowState nullableFlowState,
            SemanticModel semanticModel,
            int position,
            SymbolDisplayFormat? format = null)
        {
            format ??= SymbolDisplayFormat.MinimallyQualifiedFormat;
            return ToDisplayParts(symbol, nullableFlowState, semanticModel, position, format, minimal: true);
        }

        public static ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(
            ITypeSymbol symbol,
            CodeAnalysis.NullableAnnotation nullableAnnotation,
            SemanticModel semanticModel,
            int position,
            SymbolDisplayFormat? format = null)
        {
            format ??= SymbolDisplayFormat.MinimallyQualifiedFormat;
            return ToDisplayParts(symbol.WithNullableAnnotation(nullableAnnotation), semanticModel, position, format, minimal: true);
        }
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

        private static ImmutableArray<SymbolDisplayPart> ToDisplayParts(
            ITypeSymbol symbol,
            CodeAnalysis.NullableFlowState nullableFlowState,
            SemanticModel? semanticModelOpt,
            int positionOpt,
            SymbolDisplayFormat format,
            bool minimal)
        {
            return ToDisplayParts(symbol.WithNullableAnnotation(nullableFlowState.ToAnnotation()), semanticModelOpt, positionOpt, format, minimal);
        }

        private static ImmutableArray<SymbolDisplayPart> ToDisplayParts(
            ISymbol symbol,
            SemanticModel? semanticModelOpt,
            int positionOpt,
            SymbolDisplayFormat format,
            bool minimal)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (minimal)
            {
                if (semanticModelOpt == null)
                {
                    throw new ArgumentException(CSharpResources.SyntaxTreeSemanticModelMust);
                }
                else if (positionOpt < 0 || positionOpt > semanticModelOpt.SyntaxTree.Length) // Note: not >= since EOF is allowed.
                {
                    throw new ArgumentOutOfRangeException(CSharpResources.PositionNotWithinTree);
                }
            }
            else
            {
                Debug.Assert(semanticModelOpt == null);
                Debug.Assert(positionOpt < 0);
            }

            var builder = ArrayBuilder<SymbolDisplayPart>.GetInstance();
            var visitor = new SymbolDisplayVisitor(builder, format, semanticModelOpt, positionOpt);
            symbol.Accept(visitor);

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Returns a string representation of an object of primitive type.
        /// </summary>
        /// <param name="obj">A value to display as a string.</param>
        /// <param name="quoteStrings">Whether or not to quote string literals.</param>
        /// <param name="useHexadecimalNumbers">Whether or not to display integral literals in hexadecimal.</param>
        /// <returns>A string representation of an object of primitive type (or <see langword="null"/> if the type is not supported).</returns>
        /// <remarks>
        /// Handles <see cref="bool"/>, <see cref="string"/>, <see cref="char"/>, <see cref="sbyte"/>
        /// <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>,
        /// <see cref="long"/>, <see cref="ulong"/>, <see cref="double"/>, <see cref="float"/>, <see cref="decimal"/>,
        /// and <see langword="null"/>.
        /// </remarks>
        public static string FormatPrimitive(object obj, bool quoteStrings, bool useHexadecimalNumbers)
        {
            return ObjectDisplay.FormatPrimitive(obj, ToObjectDisplayOptions(quoteStrings, useHexadecimalNumbers));
        }

        /// <summary>
        /// Returns a C# string literal with the given value.
        /// </summary>
        /// <param name="value">The value that the resulting string literal should have.</param>
        /// <param name="quote">True to put (double) quotes around the string literal.</param>
        /// <returns>A string literal with the given value.</returns>
        /// <remarks>
        /// Escapes non-printable characters.
        /// </remarks>
        public static string FormatLiteral(string value, bool quote)
        {
            var options = ObjectDisplayOptions.EscapeNonPrintableCharacters |
                (quote ? ObjectDisplayOptions.UseQuotes : ObjectDisplayOptions.None);
            return ObjectDisplay.FormatLiteral(value, options);
        }

        /// <summary>
        /// Returns a C# character literal with the given value.
        /// </summary>
        /// <param name="c">The value that the resulting character literal should have.</param>
        /// <param name="quote">True to put (single) quotes around the character literal.</param>
        /// <returns>A character literal with the given value.</returns>
        /// <remarks>
        /// Escapes non-printable characters.
        /// </remarks>
        public static string FormatLiteral(char c, bool quote)
        {
            var options = ObjectDisplayOptions.EscapeNonPrintableCharacters |
                (quote ? ObjectDisplayOptions.UseQuotes : ObjectDisplayOptions.None);
            return ObjectDisplay.FormatLiteral(c, options);
        }

        /// <summary>
        /// Returns a textual representation of an object of primitive type as an array of string parts,
        /// each of which has a kind. Useful for colorizing the display string.
        /// </summary>
        /// <param name="obj">A value to display as string parts.</param>
        /// <returns>A list of display parts (or <see langword="default"/> if the type is not supported).</returns>
        /// <remarks>
        /// Handles <see cref="bool"/>, <see cref="string"/>, <see cref="char"/>, <see cref="sbyte"/>
        /// <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>,
        /// <see cref="long"/>, <see cref="ulong"/>, <see cref="double"/>, <see cref="float"/>, <see cref="decimal"/>,
        /// and <see langword="null"/>.
        /// </remarks>
        public static ImmutableArray<SymbolDisplayPart> PrimitiveToDisplayParts(object? obj)
        {
            return PrimitiveToDisplayParts(obj, default);
        }

        /// <summary>
        /// Returns a textual representation of an object of primitive type as an array of string parts,
        /// each of which has a kind. Useful for colorizing the display string.
        /// </summary>
        /// <param name="obj">A value to display as string parts.</param>
        /// <param name="options">Specifies the display options.</param>
        /// <returns>A list of display parts (or <see langword="default"/> if the type is not supported).</returns>
        /// <remarks>
        /// Handles <see cref="bool"/>, <see cref="string"/>, <see cref="char"/>, <see cref="sbyte"/>
        /// <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>,
        /// <see cref="long"/>, <see cref="ulong"/>, <see cref="double"/>, <see cref="float"/>, <see cref="decimal"/>,
        /// and <see langword="null"/>.
        /// </remarks>
        internal static ImmutableArray<SymbolDisplayPart> PrimitiveToDisplayParts(object? obj, SymbolDisplayConstantValueOptions options)
        {
            if (!(obj is null || obj.GetType().IsPrimitive || obj.GetType().IsEnum || obj is string || obj is decimal))
            {
                return default;
            }

            var builder = ArrayBuilder<SymbolDisplayPart>.GetInstance();
            AddConstantValue(builder, obj, ToObjectDisplayOptions(options));
            return builder.ToImmutableAndFree();
        }

        internal static void AddConstantValue(ArrayBuilder<SymbolDisplayPart> builder, object? value, SymbolDisplayConstantValueOptions options)
        {
            AddConstantValue(builder, value, ToObjectDisplayOptions(options));
        }

        private static void AddConstantValue(ArrayBuilder<SymbolDisplayPart> builder, object? value, ObjectDisplayOptions options)
        {
            if (!(value is null))
            {
                AddLiteralValue(builder, value, options);
            }
            else
            {
                builder.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, SyntaxFacts.GetText(SyntaxKind.NullKeyword)));
            }
        }

        private static void AddLiteralValue(ArrayBuilder<SymbolDisplayPart> builder, object value, ObjectDisplayOptions options)
        {
            Debug.Assert(value.GetType().IsPrimitive || value.GetType().IsEnum || value is string || value is decimal);
            var valueString = ObjectDisplay.FormatPrimitive(value, options);
            Debug.Assert(valueString != null);

            var kind = SymbolDisplayPartKind.NumericLiteral;

            switch (value)
            {
                case bool _:
                    kind = SymbolDisplayPartKind.Keyword;
                    break;
                case string _:
                case char _:
                    kind = SymbolDisplayPartKind.StringLiteral;
                    break;
            }

            builder.Add(new SymbolDisplayPart(kind, null, valueString));
        }

        private static ObjectDisplayOptions ToObjectDisplayOptions(bool quoteStrings, bool useHexadecimalNumbers)
        {
            var numberFormat = useHexadecimalNumbers ? NumericFormat.Hexadecimal : NumericFormat.Decimal;
            return ToObjectDisplayOptions(new SymbolDisplayConstantValueOptions(numberFormat, numberFormat, !quoteStrings));
        }

        private static ObjectDisplayOptions ToObjectDisplayOptions(SymbolDisplayConstantValueOptions constantValueOptions)
        {
            var options = ObjectDisplayOptions.EscapeNonPrintableCharacters;

            if (constantValueOptions.NumericLiteralFormat == NumericFormat.Hexadecimal)
                options |= ObjectDisplayOptions.UseHexadecimalNumbers;

            if (constantValueOptions.CharacterValueFormat == NumericFormat.Hexadecimal)
                options |= ObjectDisplayOptions.UseHexadecimalNumbersForCharacters;

            if (!constantValueOptions.NoQuotes)
                options |= ObjectDisplayOptions.UseQuotes;

            return options;
        }
    }
}
