﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents a text document, but optionally has a version identifier.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#optionalVersionedTextDocumentIdentifier">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class OptionalVersionedTextDocumentIdentifier : TextDocumentIdentifier, IEquatable<OptionalVersionedTextDocumentIdentifier>
    {
        /// <summary>
        /// Gets or sets the version of the document.
        /// </summary>
        [DataMember(Name = "version")]
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int? Version
        {
            get;
            set;
        }

        public static bool operator ==(OptionalVersionedTextDocumentIdentifier? value1, OptionalVersionedTextDocumentIdentifier? value2)
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

        public static bool operator !=(OptionalVersionedTextDocumentIdentifier? value1, OptionalVersionedTextDocumentIdentifier? value2)
        {
            return !(value1 == value2);
        }

        /// <inheritdoc/>
        public bool Equals(OptionalVersionedTextDocumentIdentifier other)
        {
            return other is not null
                && this.Version == other.Version
                && base.Equals(other);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is OptionalVersionedTextDocumentIdentifier other)
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
            return this.Version == null ? 89 : this.Version.GetHashCode()
                ^ (base.GetHashCode() * 79);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            // Invariant culture because the culture on the server vs client may vary.
            return base.ToString() + "|" + this.Version?.ToString(CultureInfo.InvariantCulture);
        }
    }
}
