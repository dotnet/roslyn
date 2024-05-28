// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the signature information initialization setting.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureHelpClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class SignatureInformationSetting
    {
        /// <summary>
        /// Gets or sets the set of documentation formats the client supports.
        /// </summary>
        [JsonPropertyName("documentationFormat")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MarkupKind[]? DocumentationFormat
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the parameter information the client supports.
        /// </summary>
        [JsonPropertyName("parameterInformation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ParameterInformationSetting? ParameterInformation
        {
            get;
            set;
        }
    }
}