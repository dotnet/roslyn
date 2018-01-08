// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.VirtualChars;

namespace Microsoft.CodeAnalysis.Json
{
    internal struct JsonToken
    {
        public readonly JsonKind Kind;
        public readonly ImmutableArray<JsonTrivia> LeadingTrivia;
        public readonly ImmutableArray<VirtualChar> VirtualChars;
        public readonly ImmutableArray<JsonTrivia> TrailingTrivia;
        internal readonly ImmutableArray<JsonDiagnostic> Diagnostics;
        public readonly object Value;

        public JsonToken(
            JsonKind kind,
            ImmutableArray<JsonTrivia> leadingTrivia,
            ImmutableArray<VirtualChar> virtualChars,
            ImmutableArray<JsonTrivia> trailingTrivia)
            : this(kind, leadingTrivia, virtualChars, trailingTrivia, ImmutableArray<JsonDiagnostic>.Empty)
        {
        }

        public JsonToken(
            JsonKind kind,
            ImmutableArray<JsonTrivia> leadingTrivia,
            ImmutableArray<VirtualChar> virtualChars,
            ImmutableArray<JsonTrivia> trailingTrivia,
            ImmutableArray<JsonDiagnostic> diagnostics)
            : this(kind, leadingTrivia, virtualChars, trailingTrivia, diagnostics, value: null)
        {

        }

        public JsonToken(
            JsonKind kind,
            ImmutableArray<JsonTrivia> leadingTrivia,
            ImmutableArray<VirtualChar> virtualChars,
            ImmutableArray<JsonTrivia> trailingTrivia,
            ImmutableArray<JsonDiagnostic> diagnostics, object value)
        {
            Kind = kind;
            LeadingTrivia = leadingTrivia;
            VirtualChars = virtualChars;
            TrailingTrivia = trailingTrivia;
            Diagnostics = diagnostics;
            Value = value;
        }

        public static JsonToken CreateMissing(JsonKind kind)
            => new JsonToken(kind, ImmutableArray<JsonTrivia>.Empty, ImmutableArray<VirtualChar>.Empty, ImmutableArray<JsonTrivia>.Empty);

        public bool IsMissing => VirtualChars.IsEmpty;

        public JsonToken AddDiagnosticIfNone(JsonDiagnostic diagnostic)
            => Diagnostics.Length > 0 ? this : WithDiagnostics(ImmutableArray.Create(diagnostic));

        public JsonToken WithDiagnostics(ImmutableArray<JsonDiagnostic> diagnostics)
            => With(diagnostics: diagnostics);

        public JsonToken With(
            Optional<JsonKind> kind = default,
            Optional<ImmutableArray<JsonTrivia>> leadingTrivia = default,
            Optional<ImmutableArray<VirtualChar>> virtualChars = default,
            Optional<ImmutableArray<JsonTrivia>> trailingTrivia = default,
            Optional<ImmutableArray<JsonDiagnostic>> diagnostics = default,
            Optional<object> value = default)
        {
            return new JsonToken(
                kind.HasValue ? kind.Value : Kind,
                leadingTrivia.HasValue ? leadingTrivia.Value : LeadingTrivia,
                virtualChars.HasValue ? virtualChars.Value : VirtualChars,
                trailingTrivia.HasValue ? trailingTrivia.Value : TrailingTrivia,
                diagnostics.HasValue ? diagnostics.Value : Diagnostics,
                value.HasValue ? value.Value : Value);
        }
    }
}
