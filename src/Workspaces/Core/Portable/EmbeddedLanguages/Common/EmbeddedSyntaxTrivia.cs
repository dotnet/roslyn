// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Common
{
    /// <summary>
    /// Trivia on an <see cref="EmbeddedSyntaxToken{TSyntaxKind}"/>.
    /// </summary>
    internal struct EmbeddedSyntaxTrivia
    {
        public readonly int RawKind;
        public readonly ImmutableArray<VirtualChar> VirtualChars;

        /// <summary>
        /// A place for diagnostics to be stored during parsing.  Not intended to be accessed 
        /// directly.  These will be collected and aggregated into <see cref="EmbeddedSyntaxTree{TNode, TRoot, TSyntaxKind}.Diagnostics"/>
        /// </summary> 
        internal readonly ImmutableArray<EmbeddedDiagnostic> Diagnostics;

        public EmbeddedSyntaxTrivia(int rawKind, ImmutableArray<VirtualChar> virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
        {
            Debug.Assert(virtualChars.Length > 0);
            RawKind = rawKind;
            VirtualChars = virtualChars;
            Diagnostics = diagnostics;
        }
    }
}
