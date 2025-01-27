// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Client capabilities specific to the <c>textDocument/publishDiagnostics</c> notification
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#publishDiagnosticsClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class PublishDiagnosticsSetting
    {
        /// <summary>
        /// Whether the client supports the <see cref="Diagnostic.RelatedInformation"/> property.
        /// </summary>
        public bool RelatedInformation { get; init; }

        /// <summary>
        /// Client supports the <see cref="Diagnostic.Tags"/> property to provide meta data about a diagnostic.
        /// <para>
        /// Clients supporting tags have to handle unknown tags gracefully.
        /// </para>
        /// </summary>
        [JsonPropertyName("tagSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DiagnosticTagSupport? TagSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Whether the client interprets the <see cref="PublishDiagnosticParams.Version"/> property
        /// of the <c>textDocument/publishDiagnostics</c> notification's parameter.
        /// </summary>
        /// <remarks>Since LSP 3.15</remarks>
        [JsonPropertyName("versionSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool VersionSupport { get; init; }

        /// <summary>
        /// Whether the client supports the <see cref="Diagnostic.CodeDescription"/> property.
        /// </summary>
        /// <remarks>Since LSP 3.15</remarks>
        [JsonPropertyName("codeDescriptionSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool CodeDescriptionSupport { get; init; }

        /// <summary>
        /// Whether the client supports propagating the <see cref="Diagnostic.Data"/> property from
        /// a <c>textDocument/publishDiagnostics</c> notification to a <c>textDocument/codeAction</c> request.
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("dataSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool DataSupport { get; init; }
    }
}
