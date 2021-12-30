﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
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

        public override string ToString()
        {
            using var _ = PooledStringBuilder.GetInstance(out var sb);
            WriteTo(sb, leading: false, trailing: false);
            return sb.ToString();
        }

        public string ToFullString()
        {
            using var _ = PooledStringBuilder.GetInstance(out var sb);
            WriteTo(sb, leading: true, trailing: true);
            return sb.ToString();
        }

        /// <summary>
        /// Writes the token to a stringbuilder.
        /// </summary>
        /// <param name="leading">If false, leading trivia will not be added</param>
        /// <param name="trailing">If false, trailing trivia will not be added</param>
        public void WriteTo(StringBuilder sb, bool leading, bool trailing)
        {
            if (leading)
            {
                foreach (var trivia in LeadingTrivia)
                {
                    sb.Append(trivia.ToString());
                }
            }

            sb.Append(VirtualChars.CreateString());

            if (trailing)
            {
                foreach (var trivia in TrailingTrivia)
                {
                    sb.Append(trivia.ToString());
                }
            }
        }
    }
}
