// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing
{
    internal enum SectionMatch
    {
        /// <summary>
        /// Most exact section match for a language. Will always match all files for the given language.
        /// </summary>
        /// <remarks>
        /// - for C# this is [*.cs]
        /// - for Visual Basic it is [*.vb].
        /// - If both language are specified it is [*.{cs,vb}]
        /// </remarks>
        ExactLanguageMatch = 0,
        /// <summary>
        /// Exact section match for a language with unknown file patterns. Will always match all files for the given language.
        /// </summary>
        /// <remarks>
        /// An exact match but with some unknown file patterns also matching
        /// example for C#: [*.{cs,csx}] 
        /// This will not be the case if only C# was specified and a VB pattern is found
        /// (or vice versa)
        /// </remarks>
        ExactLanguageMatchWithOthers = 1,
        /// <summary>
        /// An exact section match for a language with other known language patterns. Will match all files for the given language as well as other known languages.
        /// </summary>
        /// <remarks>
        /// Given this pattern [*.{cs,vb}] for C# this is considered a match (since it matches all C# files).
        /// Even though it also matches for Visual Basic.
        /// </remarks>
        AnyLanguageMatch = 2,
        /// <summary>
        /// Matches the file pattern according to the editorconfig specification but is a superset of an exact language match.
        /// </summary>
        /// <remarks>
        /// Patterns such as [*c*] or [*s] would match for C# in this case (being a superset of *.cs)
        /// </remarks>
        SupersetFilePatternMatch = 3,
        /// <summary>
        /// Matches the file pattern according to the editorconfig specification but is a supset of an exact language match.
        /// </summary>
        /// <remarks>
        /// Patterns such as [*.Tests.cs] would match for C# if the file being considered is UnitTests.cs
        /// </remarks>
        FilePatternMatch = 4,
        /// <summary>
        /// Matches [*].
        /// </summary>
        SplatMatch = 5,
        /// <summary>
        /// Matched because section is global and therefore always matches.
        /// </summary>
        GlobalSectionMatch = 6,

        /// <summary>
        /// Matches any valid pattern except for global section.
        /// </summary>
        AnyButGlobal = SplatMatch,
        /// <summary>
        /// Matches any valid pattern.
        /// </summary>
        Any = GlobalSectionMatch,

        /// <summary>
        /// Section did not match and is not applicable to the file or language.
        /// </summary>
        NoMatch = 100,
    }
}
