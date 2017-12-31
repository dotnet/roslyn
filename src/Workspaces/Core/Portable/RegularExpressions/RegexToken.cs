// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    internal struct RegexToken
    {
        public readonly ImmutableArray<RegexTrivia> LeadingTrivia;
        public readonly RegexKind Kind;
        public readonly ImmutableArray<VirtualChar> VirtualChars;
        internal readonly ImmutableArray<RegexDiagnostic> Diagnostics;
        public readonly object Value;

        public RegexToken(ImmutableArray<RegexTrivia> leadingTrivia, RegexKind kind, ImmutableArray<VirtualChar> virtualChars)
            : this(leadingTrivia, kind, virtualChars, ImmutableArray<RegexDiagnostic>.Empty)
        {
        }

        public RegexToken(
            ImmutableArray<RegexTrivia> leadingTrivia, RegexKind kind,
            ImmutableArray<VirtualChar> virtualChars, ImmutableArray<RegexDiagnostic> diagnostics)
            : this(leadingTrivia, kind, virtualChars, diagnostics, value: null)
        {

        }

        public RegexToken(
            ImmutableArray<RegexTrivia> leadingTrivia, RegexKind kind, ImmutableArray<VirtualChar> virtualChars,
            ImmutableArray<RegexDiagnostic> diagnostics, object value)
        {
            LeadingTrivia = leadingTrivia;
            Kind = kind;
            VirtualChars = virtualChars;
            Diagnostics = diagnostics;
            Value = value;
        }

        public static RegexToken CreateMissing(RegexKind kind)
            => new RegexToken(ImmutableArray<RegexTrivia>.Empty, kind, ImmutableArray<VirtualChar>.Empty);

        public bool IsMissing => VirtualChars.IsEmpty;

        public RegexToken AddDiagnosticIfNone(RegexDiagnostic diagnostic)
            => Diagnostics.Length > 0 ? this : WithDiagnostics(ImmutableArray.Create(diagnostic));

        public RegexToken WithDiagnostics(ImmutableArray<RegexDiagnostic> diagnostics)
            => With(diagnostics: diagnostics);

        public RegexToken With(
            Optional<ImmutableArray<RegexTrivia>> leadingTrivia = default,
            Optional<RegexKind> kind = default, 
            Optional<ImmutableArray<VirtualChar>> virtualChars = default,
            Optional<ImmutableArray<RegexDiagnostic>> diagnostics = default,
            Optional<object> value = default)
        {
            return new RegexToken(
                leadingTrivia.HasValue ? leadingTrivia.Value : LeadingTrivia,
                kind.HasValue ? kind.Value : Kind,
                virtualChars.HasValue ? virtualChars.Value : VirtualChars,
                diagnostics.HasValue ? diagnostics.Value : Diagnostics,
                value.HasValue ? value.Value : Value);
        }
    }
}
