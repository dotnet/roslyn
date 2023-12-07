// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing a filter over certain types of documents
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentFilter">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class DocumentFilter
    {
        /// <summary>
        /// Gets or sets a language id for the filter (e.g. 'typescript').
        /// </summary>
        [DataMember(Name = "language")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Language
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a Uri scheme (e.g. 'file' or 'untitled').
        /// </summary>
        [DataMember(Name = "scheme")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Scheme
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a glob pattern (e.g. '*.cs').
        /// </summary>
        [DataMember(Name = "pattern")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Pattern
        {
            get;
            set;
        }
    }
}
