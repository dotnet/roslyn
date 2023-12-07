// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents a text document, but has a version identifier.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#versionedTextDocumentIdentifier">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class VersionedTextDocumentIdentifier : TextDocumentIdentifier, IEquatable<VersionedTextDocumentIdentifier>
    {
        /// <summary>
        /// Gets or sets the version of the document.
        /// </summary>
        [DataMember(Name = "version")]
        public int Version
        {
            get;
            set;
        }

        public static bool operator ==(VersionedTextDocumentIdentifier? value1, VersionedTextDocumentIdentifier? value2)
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

        public static bool operator !=(VersionedTextDocumentIdentifier? value1, VersionedTextDocumentIdentifier? value2)
        {
            return !(value1 == value2);
        }

        /// <inheritdoc/>
        public bool Equals(VersionedTextDocumentIdentifier other)
        {
            return other is not null
                && this.Version == other.Version
                && base.Equals(other);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is VersionedTextDocumentIdentifier other)
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
            return this.Version.GetHashCode()
                ^ (base.GetHashCode() * 79);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            // Invariant culture because the culture on the server vs client may vary.
            return base.ToString() + "|" + this.Version.ToString(CultureInfo.InvariantCulture);
        }
    }
}
