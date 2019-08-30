// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A bitwise combination of the enumeration values to use when computing differences with 
    /// <see cref="IDocumentTextDifferencingService" />. 
    /// </summary>
    /// <remarks>
    /// Since computing differences can be slow with large data sets, you should not use the Character type
    /// unless the given text is relatively small.
    /// </remarks>
    [Flags]
    internal enum TextDifferenceTypes
    {
        /// <summary>
        /// Compute the line difference.
        /// </summary>
        Line = 1,

        /// <summary>
        /// Compute the word difference.
        /// </summary>
        Word = 2,

        /// <summary>
        /// Compute the character difference.
        /// </summary>
        Character = 4
    }
}
