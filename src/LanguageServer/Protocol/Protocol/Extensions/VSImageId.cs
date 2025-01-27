// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// <see cref="VSImageId"/> represents the unique identifier for a Visual Studio image asset.
    /// The identified is composed by a <see cref="Guid" /> and an integer.
    /// A list of valid image ids can be retrieved from the <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.imaging.knownmonikers">KnownMonikers class</see>
    /// from the Visual Studio SDK.
    /// </summary>
    internal class VSImageId : IEquatable<VSImageId>
    {
        /// <summary>
        /// Gets or sets the <see cref="Guid" /> component of the unique identifier.
        /// </summary>
        [JsonPropertyName("_vs_guid")]
        public Guid Guid
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the integer component of the unique identifier.
        /// </summary>
        [JsonPropertyName("_vs_id")]
        public int Id
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as VSImageId);
        }

        /// <inheritdoc/>
        public bool Equals(VSImageId? other)
        {
            return other != null &&
                   this.Guid == other.Guid &&
                   this.Id == other.Id;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 184147724;
            hashCode = (hashCode * -1521134295) + this.Guid.GetHashCode();
            hashCode = (hashCode * -1521134295) + this.Id.GetHashCode();
            return hashCode;
        }
    }
}
