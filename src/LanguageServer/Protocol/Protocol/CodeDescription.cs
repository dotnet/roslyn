// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing a description for an error code in a <see cref="Diagnostic"/>.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeDescription">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    internal class CodeDescription : IEquatable<CodeDescription>
    {
        /// <summary>
        /// Gets or sets URI to open with more information about the diagnostic error.
        /// </summary>
        [JsonPropertyName("href")]
        [JsonConverter(typeof(DocumentUriConverter))]
        public Uri Href
        {
            get;
            set;
        }

        public static bool operator ==(CodeDescription? value1, CodeDescription? value2)
        {
            if (ReferenceEquals(value1, value2))
            {
                return true;
            }

            if (ReferenceEquals(null, value2))
            {
                return false;
            }

            return value1?.Equals(value2) ?? false;
        }

        public static bool operator !=(CodeDescription? value1, CodeDescription? value2)
        {
            return !(value1 == value2);
        }

        /// <inheritdoc/>
        public bool Equals(CodeDescription other)
        {
            return other is not null
                && this.Href == other.Href;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is CodeDescription other)
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
            return this.Href == null ? 53 : this.Href.GetHashCode();
        }
    }
}
