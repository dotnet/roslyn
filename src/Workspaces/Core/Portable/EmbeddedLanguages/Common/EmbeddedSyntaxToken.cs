// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Common
{
    internal struct EmbeddedSyntaxToken
    {
        public readonly int RawKind;
        public readonly ImmutableArray<EmbeddedSyntaxTrivia> LeadingTrivia;
        public readonly ImmutableArray<VirtualChar> VirtualChars;
        public readonly ImmutableArray<EmbeddedSyntaxTrivia> TrailingTrivia;
        internal readonly ImmutableArray<EmbeddedDiagnostic> Diagnostics;
        public readonly object Value;

        public EmbeddedSyntaxToken(
            int rawKind,
            ImmutableArray<EmbeddedSyntaxTrivia> leadingTrivia,
            ImmutableArray<VirtualChar> virtualChars,
            ImmutableArray<EmbeddedSyntaxTrivia> trailingTrivia,
            ImmutableArray<EmbeddedDiagnostic> diagnostics,
            object value)
        {
            RawKind = rawKind;
            LeadingTrivia = leadingTrivia;
            VirtualChars = virtualChars;
            TrailingTrivia = trailingTrivia;
            Diagnostics = diagnostics;
            Value = value;
        }

        public bool IsMissing => VirtualChars.IsEmpty;

        public EmbeddedSyntaxToken AddDiagnosticIfNone(EmbeddedDiagnostic diagnostic)
            => Diagnostics.Length > 0 ? this : WithDiagnostics(ImmutableArray.Create(diagnostic));

        public EmbeddedSyntaxToken WithDiagnostics(ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => With(diagnostics: diagnostics);

        public EmbeddedSyntaxToken With(
            Optional<int> rawKind = default,
            Optional<ImmutableArray<EmbeddedSyntaxTrivia>> leadingTrivia = default,
            Optional<ImmutableArray<VirtualChar>> virtualChars = default,
            Optional<ImmutableArray<EmbeddedSyntaxTrivia>> trailingTrivia = default,
            Optional<ImmutableArray<EmbeddedDiagnostic>> diagnostics = default,
            Optional<object> value = default)
        {
            return new EmbeddedSyntaxToken(
                rawKind.HasValue ? rawKind.Value : RawKind,
                leadingTrivia.HasValue ? leadingTrivia.Value : LeadingTrivia,
                virtualChars.HasValue ? virtualChars.Value : VirtualChars,
                trailingTrivia.HasValue ? trailingTrivia.Value : TrailingTrivia,
                diagnostics.HasValue ? diagnostics.Value : Diagnostics,
                value.HasValue ? value.Value : Value);
        }
    }
}
