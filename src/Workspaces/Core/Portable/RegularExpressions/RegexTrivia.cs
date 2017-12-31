// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    /// <summary>
    /// Trivia on a <see cref="RegexToken"/>.  There are only two types of trivia
    /// <see cref="RegexKind.WhitespaceTrivia"/> and <see cref="RegexKind.CommentTrivia"/>.
    /// 
    /// For simplicity, all trivia is leading trivia.
    /// </summary>
    internal struct RegexTrivia
    {
        public readonly RegexKind Kind;
        public readonly ImmutableArray<VirtualChar> VirtualChars;

        /// <summary>
        /// A place for diagnostics to be stored during parsing.  Not intended to be accessed 
        /// directly.  These will be collected and aggregated into <see cref="RegexTree.Diagnostics"/>
        /// </summary> 
        internal readonly ImmutableArray<RegexDiagnostic> Diagnostics;

        public RegexTrivia(RegexKind kind, ImmutableArray<VirtualChar> virtualChars)
            : this(kind, virtualChars, ImmutableArray<RegexDiagnostic>.Empty)
        {
        }


        public RegexTrivia(RegexKind kind, ImmutableArray<VirtualChar> virtualChars, ImmutableArray<RegexDiagnostic> diagnostics)
        {
            Debug.Assert(virtualChars.Length > 0);
            Kind = kind;
            VirtualChars = virtualChars;
            Diagnostics = diagnostics;
        }
    }
}
