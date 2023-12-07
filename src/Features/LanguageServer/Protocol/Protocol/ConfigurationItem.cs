// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents an configuration item.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#configurationItem">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class ConfigurationItem
    {
        /// <summary>
        /// Gets or sets the scope to get the configuration section for.
        /// </summary>
        [DataMember(Name = "scopeUri")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DocumentUriConverter))]
        public Uri? ScopeUri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the requested configuration section.
        /// </summary>
        [DataMember(Name = "section")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Section
        {
            get;
            set;
        }
    }
}