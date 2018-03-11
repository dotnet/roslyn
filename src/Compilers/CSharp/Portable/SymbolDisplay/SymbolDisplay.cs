﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
#pragma warning disable RS0010
    /// <summary>
    /// Displays a symbol in the C# style.
    /// </summary>
    /// <seealso cref="T:Microsoft.CodeAnalysis.VisualBasic.Symbols.SymbolDisplay"/>
#pragma warning restore RS0010
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
            SymbolDisplayFormat format = null)
        {
            return ToDisplayParts(symbol, format).ToDisplayString();
        }

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
            SymbolDisplayFormat format = null)
        {
            return ToMinimalDisplayParts(symbol, semanticModel, position, format).ToDisplayString();
        }

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
            SymbolDisplayFormat format = null)
        {
            // null indicates the default format
            format = format ?? SymbolDisplayFormat.CSharpErrorMessageFormat;
            return ToDisplayParts(
                symbol, semanticModelOpt: null, positionOpt: -1, format: format, minimal: false);
        }

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
            SymbolDisplayFormat format = null)
        {
            format = format ?? SymbolDisplayFormat.MinimallyQualifiedFormat;
            return ToDisplayParts(symbol, semanticModel, position, format, minimal: true);
        }

        private static ImmutableArray<SymbolDisplayPart> ToDisplayParts(
            ISymbol symbol,
            SemanticModel semanticModelOpt,
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
        /// <returns>A string representation of an object of primitive type (or null if the type is not supported).</returns>
        /// <remarks>
        /// Handles <see cref="bool"/>, <see cref="string"/>, <see cref="char"/>, <see cref="sbyte"/>
        /// <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>,
        /// <see cref="long"/>, <see cref="ulong"/>, <see cref="double"/>, <see cref="float"/>, <see cref="decimal"/>,
        /// and <c>null</c>.
        /// </remarks>
        public static string FormatPrimitive(object obj, bool quoteStrings, bool useHexadecimalNumbers)
        {
            var options = ObjectDisplayOptions.EscapeNonPrintableCharacters;
            if (quoteStrings)
            {
                options |= ObjectDisplayOptions.UseQuotes;
            }
            if (useHexadecimalNumbers)
            {
                options |= ObjectDisplayOptions.UseHexadecimalNumbers;
            }
            return ObjectDisplay.FormatPrimitive(obj, options);
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
    }
}
