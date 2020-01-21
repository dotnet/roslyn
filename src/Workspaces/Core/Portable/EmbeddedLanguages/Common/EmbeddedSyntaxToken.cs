// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;

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
    }
}
