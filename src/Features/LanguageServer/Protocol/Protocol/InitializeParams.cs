// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents the parameter sent with an initialize method request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#initializeParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class InitializeParams
    {
        /// <summary>
        /// Gets or sets the ID of the process which launched the language server.
        /// </summary>
        [DataMember(Name = "processId")]
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public int? ProcessId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the locale the client is currently showing the user interface in.
        /// This must not necessarily be the locale of the operating system.
        ///
        /// Uses IETF language tags as the value's syntax.
        /// (See https://en.wikipedia.org/wiki/IETF_language_tag)
        /// </summary>
        [DataMember(Name = "locale")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Locale
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the workspace root path.
        /// </summary>
        [DataMember(Name = "rootPath")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [Obsolete("Deprecated in favour of RootUri")]
        public string? RootPath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the workspace root path.
        /// </summary>
        /// <remarks>
        /// This should be a string representation of an URI.
        /// </remarks>
        [DataMember(Name = "rootUri")]
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        [JsonConverter(typeof(DocumentUriConverter))]
        public Uri? RootUri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the initialization options as specified by the client.
        /// </summary>
        [DataMember(Name = "initializationOptions")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object? InitializationOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the capabilities supported by the client.
        /// </summary>
        [DataMember(Name = "capabilities")]
        public ClientCapabilities Capabilities
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the initial trace setting.
        /// </summary>
        [DataMember(Name = "trace")]
        [DefaultValue(typeof(TraceSetting), "off")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public TraceSetting Trace
        {
            get;
            set;
#pragma warning disable SA1500, SA1513 // Braces for multi-line statements should not share line, Closing brace should be followed by blank line
        } = TraceSetting.Off;
#pragma warning restore SA1500, SA1513 // Braces for multi-line statements should not share line, Closing brace should be followed by blank line
    }
}
