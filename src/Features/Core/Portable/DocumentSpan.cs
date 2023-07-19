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
    internal readonly record struct DocumentSpan
    {
        public Document Document { get; }
        public TextSpan SourceSpan { get; }

        /// <summary>
        /// Additional information attached to a document span by it creator.
        /// </summary>
        public ImmutableDictionary<string, object>? Properties { get; }

        public DocumentSpan(
            Document document,
            TextSpan sourceSpan,
            ImmutableDictionary<string, object>? properties)
        {
            Document = document;
            SourceSpan = sourceSpan;
            Properties = properties ?? ImmutableDictionary<string, object>.Empty;
        }

        public DocumentSpan(Document document, TextSpan sourceSpan)
            : this(document, sourceSpan, properties: null)
        {
        }

        public bool Equals(DocumentSpan obj)
            => Document == obj.Document && SourceSpan == obj.SourceSpan;

        public override int GetHashCode()
            => Hash.Combine(
                Document,
                SourceSpan.GetHashCode());
    }
}
