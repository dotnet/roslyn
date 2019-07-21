// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a <see cref="TextSpan"/> location in a <see cref="Document"/>.
    /// </summary>
    internal readonly struct DocumentSpan : IEquatable<DocumentSpan>
    {
        public Document Document { get; }
        public TextSpan SourceSpan { get; }

        /// <summary>
        /// Additional information attached to a document span by it creator.
        /// </summary>
        public ImmutableDictionary<string, object> Properties { get; }

        public DocumentSpan(Document document, TextSpan sourceSpan)
            : this(document, sourceSpan, properties: null)
        {
        }

        public DocumentSpan(
            Document document,
            TextSpan sourceSpan,
            ImmutableDictionary<string, object> properties)
        {
            Document = document;
            SourceSpan = sourceSpan;
            Properties = properties ?? ImmutableDictionary<string, object>.Empty;
        }

        public override bool Equals(object obj)
            => Equals((DocumentSpan)obj);

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
