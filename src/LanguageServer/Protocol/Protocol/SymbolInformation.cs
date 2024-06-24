// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing information about programming constructs like variables, classes, interfaces, etc.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#symbolInformation">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class SymbolInformation : IEquatable<SymbolInformation>
    {
        /// <summary>
        /// Gets or sets the name of this symbol.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="SymbolKind"/> of this symbol.
        /// </summary>
        [JsonPropertyName("kind")]
        public SymbolKind Kind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="Protocol.Location"/> of this symbol.
        /// </summary>
        [JsonPropertyName("location")]
        public Location Location
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the symbol containing this symbol.
        /// </summary>
        [JsonPropertyName("containerName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ContainerName
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as SymbolInformation);
        }

        /// <inheritdoc/>
        public bool Equals(SymbolInformation? other)
        {
            return other != null &&
                   this.Name == other.Name &&
                   this.Kind == other.Kind &&
                   EqualityComparer<Location>.Default.Equals(this.Location, other.Location) &&
                   this.ContainerName == other.ContainerName;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1633890234;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(this.Name);
            hashCode = (hashCode * -1521134295) + (int)this.Kind;
            hashCode = (hashCode * -1521134295) + EqualityComparer<Location>.Default.GetHashCode(this.Location);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string?>.Default.GetHashCode(this.ContainerName);
            return hashCode;
        }
    }
}
