// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Common
{
    /// <summary>
    /// Trivia on an <see cref="EmbeddedSyntaxToken{TSyntaxKind}"/>.
    /// </summary>
    internal struct EmbeddedSyntaxTrivia<TSyntaxKind> where TSyntaxKind : struct
    {
        public readonly TSyntaxKind Kind;
        public readonly VirtualCharSequence VirtualChars;

        /// <summary>
        /// A place for diagnostics to be stored during parsing.  Not intended to be accessed 
        /// directly.  These will be collected and aggregated into <see cref="EmbeddedSyntaxTree{TNode, TRoot, TSyntaxKind}.Diagnostics"/>
        /// </summary> 
        internal readonly ImmutableArray<EmbeddedDiagnostic> Diagnostics;

        public EmbeddedSyntaxTrivia(TSyntaxKind kind, VirtualCharSequence virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
        {
            Debug.Assert(virtualChars.Length > 0);
            Kind = kind;
            VirtualChars = virtualChars;
            Diagnostics = diagnostics;
        }

        public override bool Equals(object? obj)
            => obj is EmbeddedSyntaxTrivia<TSyntaxKind> trivia && this == trivia;

        public override int GetHashCode()
            => Hash.Combine(Kind.GetHashCode(),
               Hash.Combine(VirtualChars.GetHashCode(), Diagnostics.GetHashCode()));

        public static bool operator ==(EmbeddedSyntaxTrivia<TSyntaxKind> t1, EmbeddedSyntaxTrivia<TSyntaxKind> t2)
            => t1.Kind.Equals(t2.Kind)
                && t1.VirtualChars.Equals(t2.VirtualChars);

        public static bool operator !=(EmbeddedSyntaxTrivia<TSyntaxKind> t1, EmbeddedSyntaxTrivia<TSyntaxKind> t2)
            => !(t1 == t2);
    }
}
