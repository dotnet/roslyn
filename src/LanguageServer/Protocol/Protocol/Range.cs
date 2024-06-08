// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents a text document text range.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#range">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class Range : IEquatable<Range>
    {
        /// <summary>
        /// Gets or sets the text start position.
        /// </summary>
        [JsonPropertyName("start")]
        [JsonRequired]
        public Position Start
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the text end position.
        /// </summary>
        [JsonPropertyName("end")]
        [JsonRequired]
        public Position End
        {
            get;
            set;
        }

        public static bool operator ==(Range? value1, Range? value2)
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

        public static bool operator !=(Range? value1, Range? value2)
        {
            return !(value1 == value2);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as Range);
        }

        /// <inheritdoc/>
        public bool Equals(Range? other)
        {
            return other != null &&
                   EqualityComparer<Position>.Default.Equals(this.Start, other.Start) &&
                   EqualityComparer<Position>.Default.Equals(this.End, other.End);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = -1676728671;
            hashCode = (hashCode * -1521134295) + EqualityComparer<Position>.Default.GetHashCode(this.Start);
            hashCode = (hashCode * -1521134295) + EqualityComparer<Position>.Default.GetHashCode(this.End);
            return hashCode;
        }
    }
}
