// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A single element of a symbol description.  For example, a keyword, a punctuation character or
    /// a class name.
    /// </summary>
    /// <seealso cref="ISymbol.ToDisplayParts"/>
    /// <seealso cref="ISymbol.ToMinimalDisplayParts"/>
    /// <seealso cref="SymbolDisplayPartKind"/>
    public struct SymbolDisplayPart
    {
        private const string KindKey = "Kind";
        private const string TextKey = "Text";

        private readonly SymbolDisplayPartKind kind;
        private readonly string text;
        private readonly ISymbol symbol;

        /// <summary>
        /// Gets the kind of this display part.
        /// </summary>
        public SymbolDisplayPartKind Kind { get { return kind; } }

        /// <summary>
        /// Gets the symbol associated with this display part, if there is one.
        /// For example, the <see cref="ITypeSymbol"/> associated with a class name.
        /// </summary>
        /// <returns></returns>
        public ISymbol Symbol { get { return symbol; } }

        /// <summary>
        /// Construct a non-formattable <see cref="SymbolDisplayPart"/> (i.e. with a fixed string value).
        /// </summary>
        /// <param name="kind">The kind of the display part.</param>
        /// <param name="symbol">An optional associated symbol.</param>
        /// <param name="text">The fixed string value of the part.</param>
        public SymbolDisplayPart(SymbolDisplayPartKind kind, ISymbol symbol, string text)
        {
            if (!kind.IsValid())
            {
                throw new ArgumentOutOfRangeException("kind");
            }

            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            this.kind = kind;
            this.text = text;
            this.symbol = symbol;
        }

        /// <summary>
        /// Returns the string value of this symbol display part.
        /// </summary>
        public override string ToString()
        {
            return text;
        }
    }
}