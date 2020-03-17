// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the options for how constant values are displayed in the description of a symbol.
    /// This includes values of initializers and default parameters.
    /// </summary>
    public readonly struct SymbolDisplayConstantValueOptions
    {
        // We'd like the default value of this struct to have a CharacterValueFormat of Hexadecimal as opposed to Decimal
        // for compatibility reasons (this used to be the behavior before these options were introduced).
        private readonly NumericFormat? _characterValueFormat;

        public SymbolDisplayConstantValueOptions(NumericFormat numericLiteralFormat, NumericFormat characterValueFormat, bool noQuotes)
        {
            NumericLiteralFormat = numericLiteralFormat;
            _characterValueFormat = characterValueFormat;
            NoQuotes = noQuotes;
        }

        /// <summary>
        /// Specifies how values of integer types are displayed.
        /// </summary>
        public NumericFormat NumericLiteralFormat { get; }

        /// <summary>
        /// For Visual Basic, specifies how numeric values of characters inside ChrW are displayed.
        /// </summary>
        public NumericFormat CharacterValueFormat => _characterValueFormat ?? NumericFormat.Hexadecimal;

        /// <summary>
        /// If <see langword="true"/>, values of type <see cref="string"/> and <see cref="char"/> are displayed
        /// without quotes and without characters being escaped.
        /// </summary>
        public bool NoQuotes { get; }
    }
}
