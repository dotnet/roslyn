// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Common
{
    internal struct EmbeddedSyntaxToken<TSyntaxKind> where TSyntaxKind : struct
    {
        public readonly TSyntaxKind Kind;
        public readonly ImmutableArray<EmbeddedSyntaxTrivia> LeadingTrivia;
        public readonly ImmutableArray<VirtualChar> VirtualChars;
        public readonly ImmutableArray<EmbeddedSyntaxTrivia> TrailingTrivia;
        internal readonly ImmutableArray<EmbeddedDiagnostic> Diagnostics;
        public readonly object Value;

        public EmbeddedSyntaxToken(
            TSyntaxKind kind,
            ImmutableArray<EmbeddedSyntaxTrivia> leadingTrivia,
            ImmutableArray<VirtualChar> virtualChars,
            ImmutableArray<EmbeddedSyntaxTrivia> trailingTrivia)
            : this(kind, leadingTrivia, virtualChars, trailingTrivia, ImmutableArray<EmbeddedDiagnostic>.Empty)
        {
        }

        public EmbeddedSyntaxToken(
            TSyntaxKind kind,
            ImmutableArray<EmbeddedSyntaxTrivia> leadingTrivia,
            ImmutableArray<VirtualChar> virtualChars,
            ImmutableArray<EmbeddedSyntaxTrivia> trailingTrivia,
            ImmutableArray<EmbeddedDiagnostic> diagnostics)
            : this(kind, leadingTrivia, virtualChars, trailingTrivia, diagnostics, value: null)
        {
        }

        public EmbeddedSyntaxToken(
            TSyntaxKind kind,
            ImmutableArray<EmbeddedSyntaxTrivia> leadingTrivia,
            ImmutableArray<VirtualChar> virtualChars,
            ImmutableArray<EmbeddedSyntaxTrivia> trailingTrivia,
            ImmutableArray<EmbeddedDiagnostic> diagnostics, object value)
        {
            Kind = kind;
            LeadingTrivia = leadingTrivia;
            VirtualChars = virtualChars;
            TrailingTrivia = trailingTrivia;
            Diagnostics = diagnostics;
            Value = value;
        }

        public static EmbeddedSyntaxToken<TSyntaxKind> CreateMissing(TSyntaxKind kind)
            => new EmbeddedSyntaxToken<TSyntaxKind>(kind, ImmutableArray<EmbeddedSyntaxTrivia>.Empty, ImmutableArray<VirtualChar>.Empty, ImmutableArray<EmbeddedSyntaxTrivia>.Empty);

        public bool IsMissing => VirtualChars.IsEmpty;

        public EmbeddedSyntaxToken<TSyntaxKind> AddDiagnosticIfNone(EmbeddedDiagnostic diagnostic)
            => Diagnostics.Length > 0 ? this : WithDiagnostics(ImmutableArray.Create(diagnostic));

        public EmbeddedSyntaxToken<TSyntaxKind> WithDiagnostics(ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => With(diagnostics: diagnostics);

        public EmbeddedSyntaxToken<TSyntaxKind> With(
            Optional<TSyntaxKind> kind = default,
            Optional<ImmutableArray<EmbeddedSyntaxTrivia>> leadingTrivia = default,
            Optional<ImmutableArray<VirtualChar>> virtualChars = default,
            Optional<ImmutableArray<EmbeddedSyntaxTrivia>> trailingTrivia = default,
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
    }
}
