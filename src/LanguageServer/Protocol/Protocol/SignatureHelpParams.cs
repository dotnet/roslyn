// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the parameters for the textDocument/signatureHelp request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureHelpParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class SignatureHelpParams : TextDocumentPositionParams
    {
        /// <summary>
        /// Gets or sets the signature help context.
        /// </summary>
        [JsonPropertyName("context")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SignatureHelpContext? Context
        {
            get;
            set;
        }
    }
}
