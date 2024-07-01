// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.ComponentModel;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents the parameter sent with an initialize method request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#initializeParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class InitializeParams
    {
        /// <summary>
        /// Gets or sets the ID of the process which launched the language server.
        /// </summary>
        [JsonPropertyName("processId")]
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
        [JsonPropertyName("locale")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Locale
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the workspace root path.
        /// </summary>
        [JsonPropertyName("rootPath")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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
        [JsonPropertyName("rootUri")]
        [JsonConverter(typeof(DocumentUriConverter))]
        public Uri? RootUri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the initialization options as specified by the client.
        /// </summary>
        [JsonPropertyName("initializationOptions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? InitializationOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the capabilities supported by the client.
        /// </summary>
        [JsonPropertyName("capabilities")]
        public ClientCapabilities Capabilities
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the initial trace setting.
        /// </summary>
        [JsonPropertyName("trace")]
        [DefaultValue(typeof(TraceSetting), "off")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public TraceSetting Trace
        {
            get;
            set;
#pragma warning disable SA1500, SA1513 // Braces for multi-line statements should not share line, Closing brace should be followed by blank line
        } = TraceSetting.Off;
#pragma warning restore SA1500, SA1513 // Braces for multi-line statements should not share line, Closing brace should be followed by blank line
    }
}
