// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal enum SearchKind
    {
        /// <summary>
        /// Use an case-sensitive comparison when searching for matching items.
        /// </summary>
        Exact,

        /// <summary>
        /// Use a case-insensitive comparison when searching for matching items.
        /// </summary>
        ExactIgnoreCase,

        /// <summary>
        /// Use a fuzzy comparison when searching for matching items. Fuzzy matching allows for 
        /// a certain amount of misspellings, missing words, etc. See <see cref="SpellChecker"/> for 
        /// more details.
        /// </summary>
        Fuzzy,

        /// <summary>
        /// Search term is matched in a custom manner (i.e. with a user provided predicate).
        /// </summary>
        Custom
    }
}
