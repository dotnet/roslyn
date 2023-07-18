// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a <see cref="TextSpan"/> location in a <see cref="Document"/>.
    /// </summary>
    internal readonly struct DocumentSpan(
        Document document,
        TextSpan sourceSpan,
        ImmutableDictionary<string, object>? properties) : IEquatable<DocumentSpan>
    {
        public Document Document { get; } = document;
        public TextSpan SourceSpan { get; } = sourceSpan;

        /// <summary>
        /// Additional information attached to a document span by it creator.
        /// </summary>
        public ImmutableDictionary<string, object>? Properties { get; } = properties ?? ImmutableDictionary<string, object>.Empty;

        public DocumentSpan(Document document, TextSpan sourceSpan)
            : this(document, sourceSpan, properties: null)
        {
        }

        public override bool Equals(object? obj)
            => obj is DocumentSpan documentSpan && Equals(documentSpan);

        public bool Equals(DocumentSpan obj)
            => Document == obj.Document && SourceSpan == obj.SourceSpan;

        public static bool operator ==(DocumentSpan d1, DocumentSpan d2)
            => d1.Equals(d2);

        public static bool operator !=(DocumentSpan d1, DocumentSpan d2)
            => !(d1 == d2);

        public override int GetHashCode()
            => Hash.Combine(
                Document,
                SourceSpan.GetHashCode());
    }
}
