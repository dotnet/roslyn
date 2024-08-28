// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents an configuration item.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#configurationItem">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class ConfigurationItem
    {
        /// <summary>
        /// Gets or sets the scope to get the configuration section for.
        /// </summary>
        [JsonPropertyName("scopeUri")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonConverter(typeof(DocumentUriConverter))]
        public Uri? ScopeUri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the requested configuration section.
        /// </summary>
        [JsonPropertyName("section")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Section
        {
            get;
            set;
        }
    }
}
