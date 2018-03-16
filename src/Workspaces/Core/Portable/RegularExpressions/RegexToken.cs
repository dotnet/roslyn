// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    internal static class EmbeddedSyntaxTokenExtensions
    {
        public static RegexKind Kind(this EmbeddedSyntaxToken token)
            => (RegexKind)token.RawKind;
    }
}

//    internal struct RegexToken
//    {
//        public readonly RegexKind Kind;
//        public readonly ImmutableArray<RegexTrivia> LeadingTrivia;
//        public readonly ImmutableArray<VirtualChar> VirtualChars;
//        internal readonly ImmutableArray<RegexDiagnostic> Diagnostics;
//        public readonly object Value;

//        public RegexToken(RegexKind kind, ImmutableArray<RegexTrivia> leadingTrivia, ImmutableArray<VirtualChar> virtualChars)
//            : this(kind, leadingTrivia, virtualChars, ImmutableArray<RegexDiagnostic>.Empty)
//        {
//        }

//        public RegexToken(
//            RegexKind kind, ImmutableArray<RegexTrivia> leadingTrivia, 
//            ImmutableArray<VirtualChar> virtualChars, ImmutableArray<RegexDiagnostic> diagnostics)
//            : this(kind, leadingTrivia, virtualChars, diagnostics, value: null)
//        {

//        }

//        public RegexToken(
//            RegexKind kind, ImmutableArray<RegexTrivia> leadingTrivia, ImmutableArray<VirtualChar> virtualChars,
//            ImmutableArray<RegexDiagnostic> diagnostics, object value)
//        {
//            Kind = kind;
//            LeadingTrivia = leadingTrivia;
//            VirtualChars = virtualChars;
//            Diagnostics = diagnostics;
//            Value = value;
//        }

//        public static RegexToken CreateMissing(RegexKind kind)
//            => new RegexToken(kind, ImmutableArray<RegexTrivia>.Empty, ImmutableArray<VirtualChar>.Empty);

//        public bool IsMissing => VirtualChars.IsEmpty;

//        public RegexToken AddDiagnosticIfNone(RegexDiagnostic diagnostic)
//            => Diagnostics.Length > 0 ? this : WithDiagnostics(ImmutableArray.Create(diagnostic));

//        public RegexToken WithDiagnostics(ImmutableArray<RegexDiagnostic> diagnostics)
//            => With(diagnostics: diagnostics);

//        public RegexToken With(
//            Optional<RegexKind> kind = default,
//            Optional<ImmutableArray<RegexTrivia>> leadingTrivia = default,
//            Optional<ImmutableArray<VirtualChar>> virtualChars = default,
//            Optional<ImmutableArray<RegexDiagnostic>> diagnostics = default,
//            Optional<object> value = default)
//        {
//            return new RegexToken(
//                kind.HasValue ? kind.Value : Kind,
//                leadingTrivia.HasValue ? leadingTrivia.Value : LeadingTrivia,
//                virtualChars.HasValue ? virtualChars.Value : VirtualChars,
//                diagnostics.HasValue ? diagnostics.Value : Diagnostics,
//                value.HasValue ? value.Value : Value);
//        }
//    }
//}
