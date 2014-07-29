// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Whether or not to quote character and string literals. In Visual Basic, this also enables pretty-listing of non-printable characters using ChrW function and vb* constants.
        /// </summary>
        UseQuotes = 1 << 3,
    }
}
