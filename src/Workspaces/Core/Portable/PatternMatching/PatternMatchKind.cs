﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        /// <summary>
        /// All camel-humps in the pattern matched a camel-hump in the candidate.  All camel-humps
        /// in the candidate were matched by a camel-hump in the pattern.
        /// 
        /// Example: "CFPS" matching "CodeFixProviderService"
        /// Example: "cfps" matching "CodeFixProviderService"
        /// Example: "CoFiPrSe" matching "CodeFixProviderService"
        /// </summary>
        CamelCaseExact,

        /// <summary>
        /// All camel-humps in the pattern matched a camel-hump in the candidate.  The first camel-hump
        /// in the pattern matched the first camel-hump in the candidate.  There was no gap in the camel-
        /// humps in the candidate that were matched.
        ///
        /// Example: "CFP" matching "CodeFixProviderService"
        /// Example: "cfp" matching "CodeFixProviderService"
        /// Example: "CoFiPRo" matching "CodeFixProviderService"
        /// </summary>
        CamelCasePrefix,

        /// <summary>
        /// All camel-humps in the pattern matched a camel-hump in the candidate.  The first camel-hump
        /// in the pattern matched the first camel-hump in the candidate.  There was at least one gap in 
        /// the camel-humps in the candidate that were matched.
        ///
        /// Example: "CP" matching "CodeFixProviderService"
        /// Example: "cp" matching "CodeFixProviderService"
        /// Example: "CoProv" matching "CodeFixProviderService"
        /// </summary>
        CamelCaseNonContiguousPrefix,

        /// <summary>
        /// All camel-humps in the pattern matched a camel-hump in the candidate.  The first camel-hump
        /// in the pattern did not match the first camel-hump in the pattern.  There was no gap in the camel-
        /// humps in the candidate that were matched.
        ///
        /// Example: "FP" matching "CodeFixProviderService"
        /// Example: "fp" matching "CodeFixProviderService"
        /// Example: "FixPro" matching "CodeFixProviderService"
        /// </summary>
        CamelCaseSubstring,

        /// <summary>
        /// All camel-humps in the pattern matched a camel-hump in the candidate.  The first camel-hump
        /// in the pattern did not match the first camel-hump in the pattern.  There was at least one gap in 
        /// the camel-humps in the candidate that were matched.
        ///
        /// Example: "FS" matching "CodeFixProviderService"
        /// Example: "fs" matching "CodeFixProviderService"
        /// Example: "FixSer" matching "CodeFixProviderService"
        /// </summary>
        CamelCaseNonContiguousSubstring,

        /// <summary>
        /// The pattern matches the candidate in a fuzzy manner.  Fuzzy matching allows for 
        /// a certain amount of misspellings, missing words, etc. See <see cref="SpellChecker"/> for 
        /// more details.
        /// </summary>
        Fuzzy
    }
}
