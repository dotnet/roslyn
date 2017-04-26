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

        // Note: CamelCased matches are ordered from best to worst.

        CamelCaseExact,
        CamelCasePrefix,
        CamelCaseNonContiguousPrefix,
        CamelCaseSubstring,
        CamelCaseNonContiguousSubstring,

#if false
        /// <summary>
        /// The pattern matched the CamelCased candidate string.  All humps in the pattern matched humps in
        /// the candidate in order.  The first hump in the pattern matched the first hump in the candidate 
        /// string.  There were no unmatched humps in the candidate between matched humps.
        /// 
        /// Examples: "CFP" matching "CodeFixProvider" as well as "CFP" matching "CodeFixProviderService".
        /// </summary>
        CamelCaseContiguousFromStart,

        /// <summary>
        /// The pattern matched the CamelCased candidate string.  All humps in the pattern matched some hump in
        /// the candidate in order.  The first hump in the pattern matched the first hump in the candidate string.  
        /// There was at least one hump in the candidate that was not matched that was between humps in the
        /// candidate that were matched.
        ///
        /// Examples: "CPS" matching "CodeFixProviderService".  Here 'Fix' was not matched, so the result was
        /// not 'Contiguous'.
        /// </summary>
        CamelCaseFromStart,

        /// <summary>
        /// The pattern matched the CamelCased candidate string.  All humps in the pattern matched humps in
        /// the candidate in order.  The first hump in the pattern did not match the first hump in the candidate
        /// string.  There were no unmatched humps in the candidate between matched humps.
        ///
        /// Example: "FP" matching "CodeFixProviderService"
        /// </summary>
        CamelCaseContiguous,

        /// <summary>
        /// The pattern matched the CamelCased candidate string.  All humps in the pattern matched some hump in
        /// the candidate in order.  The first hump in the pattern did not match the first hump in the candidate
        /// string.  There was at least one hump in the candidate that was not matched that was between humps 
        /// in the candidate that were matched.
        ///
        /// Example: "FS" matching "CodeFixProviderService".  Because 'Code' was not matched, the match was not
        /// a 'FromStart' match.  Because 'Provider' was not matched, the match was not a 'Contiguous' match.
        /// </summary>
        CamelCase,
#endif

        /// <summary>
        /// The pattern matches the candidate in a fuzzy manner.  Fuzzy matching allows for 
        /// a certain amount of misspellings, missing words, etc. See <see cref="SpellChecker"/> for 
        /// more details.
        /// </summary>
        Fuzzy
    }
}
