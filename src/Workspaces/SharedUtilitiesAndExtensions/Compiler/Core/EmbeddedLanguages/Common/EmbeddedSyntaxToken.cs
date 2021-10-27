// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Common
{
    internal struct EmbeddedSyntaxToken<TSyntaxKind> where TSyntaxKind : struct
    {
        public readonly TSyntaxKind Kind;
        public readonly ImmutableArray<EmbeddedSyntaxTrivia<TSyntaxKind>> LeadingTrivia;
        public readonly VirtualCharSequence VirtualChars;
        public readonly ImmutableArray<EmbeddedSyntaxTrivia<TSyntaxKind>> TrailingTrivia;
        internal readonly ImmutableArray<EmbeddedDiagnostic> Diagnostics;

        /// <summary>
        /// Returns the value of the token. For example, if the token represents an integer capture,
        /// then this property would return the actual integer.
        /// </summary>
        public readonly object Value;

        public EmbeddedSyntaxToken(
            TSyntaxKind kind,
            ImmutableArray<EmbeddedSyntaxTrivia<TSyntaxKind>> leadingTrivia,
            VirtualCharSequence virtualChars,
            ImmutableArray<EmbeddedSyntaxTrivia<TSyntaxKind>> trailingTrivia,
            ImmutableArray<EmbeddedDiagnostic> diagnostics, object value)
        {
            Debug.Assert(!leadingTrivia.IsDefault);
            Debug.Assert(!virtualChars.IsDefault);
            Debug.Assert(!trailingTrivia.IsDefault);
            Debug.Assert(!diagnostics.IsDefault);
            Kind = kind;
            LeadingTrivia = leadingTrivia;
            VirtualChars = virtualChars;
            TrailingTrivia = trailingTrivia;
            Diagnostics = diagnostics;
            Value = value;
        }

        public bool IsMissing => VirtualChars.IsEmpty;

        public EmbeddedSyntaxToken<TSyntaxKind> AddDiagnosticIfNone(EmbeddedDiagnostic diagnostic)
            => Diagnostics.Length > 0 ? this : WithDiagnostics(ImmutableArray.Create(diagnostic));

        public EmbeddedSyntaxToken<TSyntaxKind> WithDiagnostics(ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => With(diagnostics: diagnostics);

        public EmbeddedSyntaxToken<TSyntaxKind> With(
            Optional<TSyntaxKind> kind = default,
            Optional<ImmutableArray<EmbeddedSyntaxTrivia<TSyntaxKind>>> leadingTrivia = default,
            Optional<VirtualCharSequence> virtualChars = default,
            Optional<ImmutableArray<EmbeddedSyntaxTrivia<TSyntaxKind>>> trailingTrivia = default,
            Optional<ImmutableArray<EmbeddedDiagnostic>> diagnostics = default,
            Optional<object> value = default)
        {
            return new EmbeddedSyntaxToken<TSyntaxKind>(
                kind.HasValue ? kind.Value : Kind,
                leadingTrivia.HasValue ? leadingTrivia.Value : LeadingTrivia,
                virtualChars.HasValue ? virtualChars.Value : VirtualChars,
                trailingTrivia.HasValue ? trailingTrivia.Value : TrailingTrivia,
                diagnostics.HasValue ? diagnostics.Value : Diagnostics,
                value.HasValue ? value.Value : Value);
        }

        public TextSpan GetSpan()
            => EmbeddedSyntaxHelpers.GetSpan(this.VirtualChars);

        public static bool operator ==(EmbeddedSyntaxToken<TSyntaxKind> t1, EmbeddedSyntaxToken<TSyntaxKind> t2)
            => t1.Kind.Equals(t2.Kind)
                && t1.LeadingTrivia.IsDefault == t2.LeadingTrivia.IsDefault
                && (t1.LeadingTrivia.IsDefault || t1.LeadingTrivia.SequenceEqual(t2.LeadingTrivia))
                && t1.VirtualChars == t2.VirtualChars
                && t1.TrailingTrivia.IsDefault == t2.TrailingTrivia.IsDefault 
                && (t1.TrailingTrivia.IsDefault || t1.TrailingTrivia.SequenceEqual(t2.TrailingTrivia))
                && t1.Diagnostics.IsDefault == t2.Diagnostics.IsDefault
                && (t1.Diagnostics.IsDefault || t1.Diagnostics.SequenceEqual(t2.Diagnostics))
                && t1.Value == t2.Value;

        public static bool operator !=(EmbeddedSyntaxToken<TSyntaxKind> t1, EmbeddedSyntaxToken<TSyntaxKind> t2)
            => !(t1 == t2);

        public override bool Equals(object? obj)
            => obj is EmbeddedSyntaxToken<TSyntaxKind> t && this == t;

        public override int GetHashCode()
            => Hash.Combine(Kind.GetHashCode(),
                Hash.Combine(LeadingTrivia.GetHashCode(),
                Hash.Combine(VirtualChars.GetHashCode(),
                Hash.Combine(TrailingTrivia.GetHashCode(),
                Hash.Combine(Diagnostics.GetHashCode(), Value?.GetHashCode() ?? 0)))));
    }
}
