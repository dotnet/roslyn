// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the signature information initialization setting.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureHelpClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class SignatureInformationSetting
    {
        /// <summary>
        /// The client supports the following content formats for the <see cref="SignatureInformation.Documentation"/>
        /// property. The order describes the preferred format of the client.
        /// </summary>
        [JsonPropertyName("documentationFormat")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MarkupKind[]? DocumentationFormat
        {
            get;
            set;
        }

        /// <summary>
        /// Client capabilities specific to <see cref="ParameterInformation"/>
        /// </summary>
        [JsonPropertyName("parameterInformation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ParameterInformationSetting? ParameterInformation
        {
            get;
            set;
        }

        /// <summary>
        /// The client supports the <see cref="SignatureInformation.ActiveParameter"/> property
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("activeParameterSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool ActiveParameterSupport { get; init; }
    }
}
