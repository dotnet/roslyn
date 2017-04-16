// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PatternMatching
{
    /// <summary>
    /// Note(cyrusn): this enum is ordered from strongest match type to weakest match type.
    /// </summary>
    internal enum PatternMatchKind
    {
        /// <summary>
        /// The candidate string matched the pattern exactly.
        /// </summary>
        Exact,

        /// <summary>
        /// The pattern was a prefix of the candidate string.
        /// </summary>
        Prefix,

        /// <summary>
        /// The pattern was a substring of the candidate string, but in a way that wasn't a CamelCase match.
        /// </summary>
        Substring,

        /// <summary>
        /// The pattern matched the CamelCased candidate string.  The pattern matched the first
        /// camel-hump and matched all following humps.
        /// </summary>
        CamelCaseContiguousFromStart,

        /// <summary>
        /// The pattern matched the CamelCased candidate string.  The pattern matched the first
        /// camel-hump but didn't match all following humps.
        /// </summary>
        CamelCaseFromStart,

        /// <summary>
        /// The pattern matched the CamelCased candidate string.  The pattern did not match the
        /// first camel-hump, but once it matched all humps were contiguous.
        /// </summary>
        CamelCaseContiguous,

        /// <summary>
        /// The pattern matched the CamelCased candidate string.  The pattern did not match the
        /// first camel-hump, and not all the humps were were contiguous.
        /// </summary>
        CamelCase,

        /// <summary>
        /// The pattern matches the candidate in a fuzzy manner.  Fuzzy matching allows for 
        /// a certain amount of misspellings, missing words, etc. See <see cref="SpellChecker"/> for 
        /// more details.
        /// </summary>
        Fuzzy
    }
}
