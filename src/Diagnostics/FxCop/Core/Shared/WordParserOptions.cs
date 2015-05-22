﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.AnalyzerPowerPack.Utilities
{
    /// <summary>
    ///   Defines the word parsing and delimiting options for use with <see cref="WordParser.Parse(String,WordParserOptions)"/>.
    /// </summary>
    [Flags]
    internal enum WordParserOptions
    {
        /// <summary>
        ///   Indicates the default options for word parsing.
        /// </summary>
        None = 0,

        /// <summary>
        ///   Indicates that <see cref="WordParser.Parse(String,WordParserOptions)"/> should ignore the mnemonic indicator characters (&amp;) embedded within words.
        /// </summary>
        IgnoreMnemonicsIndicators = 1,

        /// <summary>
        ///   Indicates that <see cref="WordParser.Parse(String,WordParserOptions)"/> should split compound words.
        /// </summary>
        SplitCompoundWords = 2,
    }
}
