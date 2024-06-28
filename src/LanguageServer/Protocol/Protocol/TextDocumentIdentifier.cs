// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which identifies a text document.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentIdentifier">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class TextDocumentIdentifier : IEquatable<TextDocumentIdentifier>
    {
        /// <summary>
        /// Gets or sets the URI of the text document.
        /// </summary>
        [JsonPropertyName("uri")]
        [JsonConverter(typeof(DocumentUriConverter))]
        public Uri Uri
        {
            get;
            set;
        }

        public static bool operator ==(TextDocumentIdentifier? value1, TextDocumentIdentifier? value2)
        {
            if (ReferenceEquals(value1, value2))
            {
                return true;
            }

            // Is null?
            if (ReferenceEquals(null, value2))
            {
                return false;
            }

            return value1?.Equals(value2) ?? false;
        }

        public static bool operator !=(TextDocumentIdentifier? value1, TextDocumentIdentifier? value2)
        {
            return !(value1 == value2);
        }

        /// <inheritdoc/>
        public bool Equals(TextDocumentIdentifier other)
        {
            return other is not null
                && this.Uri == other.Uri;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is TextDocumentIdentifier other)
            {
                return this.Equals(other);
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.Uri == null ? 89 : this.Uri.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Uri == null ? string.Empty : this.Uri.AbsolutePath;
        }
    }
}
