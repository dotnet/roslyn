// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// Options for SuppressOperation
    /// 
    /// NoWrappingIfOnSingleLine means no wrapping if given tokens are on same line
    /// NoWrapping means no wrapping regardless of relative positions of two tokens
    /// NoSpacing means no spacing regardless of relative positions of two tokens
    /// 
    /// </summary>
    [Flags]
    internal enum SuppressOption
    {
        None = 0x0,

        NoWrappingIfOnSingleLine = 0x1,
        NoWrappingIfOnMultipleLine = 0x2,
        NoWrapping = NoWrappingIfOnSingleLine | NoWrappingIfOnMultipleLine,
        NoSpacingIfOnSingleLine = 0x4,
        NoSpacingIfOnMultipleLine = 0x8,
        NoSpacing = NoSpacingIfOnSingleLine | NoSpacingIfOnMultipleLine,

        // a suppression operation containing elastic trivia in its start/end token will be ignored
        // since they can't be used to determine line alignment between two tokens.
        // this option will make engine to accept the operation even if start/end token has elastic trivia
        IgnoreElasticWrapping = 0x10,

        /// <summary>
        /// Completely disable formatting within a span.
        /// </summary>
        DisableFormatting = 0x20,
    }
}
