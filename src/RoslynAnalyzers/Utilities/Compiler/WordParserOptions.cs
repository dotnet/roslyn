// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Analyzer.Utilities
{
    /// <summary>
    ///   Defines the word parsing and delimiting options for use with <see cref="WordParser.Parse(string,WordParserOptions)"/>.
    /// </summary>
    [Flags]
    internal enum WordParserOptions
    {
        /// <summary>
        ///   Indicates the default options for word parsing.
        /// </summary>
        None = 0,

        /// <summary>
        ///   Indicates that <see cref="WordParser.Parse(string,WordParserOptions)"/> should ignore the mnemonic indicator characters (&amp;) embedded within words.
        /// </summary>
        IgnoreMnemonicsIndicators = 1,

        /// <summary>
        ///   Indicates that <see cref="WordParser.Parse(string,WordParserOptions)"/> should split compound words.
        /// </summary>
        SplitCompoundWords = 2,
    }
}
