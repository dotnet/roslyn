// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Extension class for ClientCapabilities with fields specific to Visual Studio.
    /// </summary>
    [DataContract]
    internal class VSInternalClientCapabilities : ClientCapabilities
    {
        /// <summary>
        /// Gets or sets a value indicating whether client supports Visual Studio extensions.
        /// </summary>
        [DataMember(Name = "_vs_supportsVisualStudioExtensions")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool SupportsVisualStudioExtensions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating what level of snippet support is available from Visual Studio Client.
        /// v1.0 refers to only default tab stop support i.e. support for $0 which manipualtes the cursor position.
        /// </summary>
        [DataMember(Name = "_vs_supportedSnippetVersion")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalSnippetSupportLevel? SupportedSnippetVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether client supports omitting document text in textDocument/didOpen notifications.
        /// </summary>
        [DataMember(Name = "_vs_supportsNotIncludingTextInTextDocumentDidOpen")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool SupportsNotIncludingTextInTextDocumentDidOpen
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the client supports string based response kinds
        /// instead of enum based response kinds.
        /// </summary>
        [DataMember(Name = "_vs_supportsIconExtensions")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool SupportsIconExtensions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the client provides support for diagnostic pull requests.
        /// </summary>
        [DataMember(Name = "_vs_supportsDiagnosticRequests")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool SupportsDiagnosticRequests
        {
            get;
            set;
        }
    }
}