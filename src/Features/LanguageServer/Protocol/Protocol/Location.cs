// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing a location in a document.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#location">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class Location : IEquatable<Location>
    {
        /// <summary>
        /// Gets or sets the URI for the document the location belongs to.
        /// </summary>
        [DataMember(Name = "uri")]
        [JsonConverter(typeof(DocumentUriConverter))]
        public Uri Uri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the range of the location in the document.
        /// </summary>
        [DataMember(Name = "range")]
        public Range Range
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as Location);
        }

        /// <inheritdoc/>
        public bool Equals(Location? other)
        {
            return other != null && this.Uri != null && other.Uri != null &&
                   this.Uri.Equals(other.Uri) &&
                   EqualityComparer<Range>.Default.Equals(this.Range, other.Range);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1486144663;
            hashCode = (hashCode * -1521134295) + EqualityComparer<Uri>.Default.GetHashCode(this.Uri);
            hashCode = (hashCode * -1521134295) + EqualityComparer<Range>.Default.GetHashCode(this.Range);
            return hashCode;
        }
    }
}
