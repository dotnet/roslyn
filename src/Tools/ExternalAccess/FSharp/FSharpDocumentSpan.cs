﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp
{
    /// <summary>
    /// Represents a <see cref="TextSpan"/> location in a <see cref="Document"/>.
    /// </summary>
    internal readonly struct FSharpDocumentSpan : IEquatable<FSharpDocumentSpan>
    {
        public Document Document { get; }
        public TextSpan SourceSpan { get; }

        /// <summary>
        /// Additional information attached to a document span by it creator.
        /// </summary>
        public ImmutableDictionary<string, object> Properties { get; }

        public FSharpDocumentSpan(Document document, TextSpan sourceSpan)
            : this(document, sourceSpan, properties: null)
        {
        }

        public FSharpDocumentSpan(
            Document document,
            TextSpan sourceSpan,
            ImmutableDictionary<string, object> properties)
        {
            Document = document;
            SourceSpan = sourceSpan;
            Properties = properties ?? ImmutableDictionary<string, object>.Empty;
        }

        public override bool Equals(object obj)
            => Equals((FSharpDocumentSpan)obj);

        public bool Equals(FSharpDocumentSpan obj)
            => this.Document == obj.Document && this.SourceSpan == obj.SourceSpan;

        public static bool operator ==(FSharpDocumentSpan d1, FSharpDocumentSpan d2)
            => d1.Equals(d2);

        public static bool operator !=(FSharpDocumentSpan d1, FSharpDocumentSpan d2)
            => !(d1 == d2);

        public override int GetHashCode()
            => Hash.Combine(
                this.Document,
                this.SourceSpan.GetHashCode());

        internal Microsoft.CodeAnalysis.DocumentSpan ToRoslynDocumentSpan()
        {
            return new Microsoft.CodeAnalysis.DocumentSpan(this.Document, this.SourceSpan, this.Properties);
        }
    }
}
