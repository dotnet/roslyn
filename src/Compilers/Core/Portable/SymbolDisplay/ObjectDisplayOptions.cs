// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the options for how generics are displayed in the description of a symbol.
    /// </summary>
    [Flags]
    internal enum ObjectDisplayOptions
    {
        /// <summary>
        /// Format object using default options.
        /// </summary>
        None = 0,

        /// <summary>
        /// In C#, include the numeric code point before character literals.
        /// </summary>
        IncludeCodePoints = 1 << 0,

        /// <summary>
        /// Whether or not to include type suffix for applicable integral literals.
        /// </summary>
        IncludeTypeSuffix = 1 << 1,

        /// <summary>
        /// Whether or not to display integral literals in hexadecimal.
        /// </summary>
        UseHexadecimalNumbers = 1 << 2,

        /// <summary>
        /// Whether or not to quote character and string literals.
        /// </summary>
        UseQuotes = 1 << 3,

        /// <summary>
        /// In C#, replace non-printable (e.g. control) characters with dedicated (e.g. \t) or unicode (\u0001) escape sequences.
        /// In Visual Basic, replace non-printable characters with calls to ChrW and vb* constants.
        /// </summary>
        EscapeNonPrintableCharacters = 1 << 4,
    }
}
