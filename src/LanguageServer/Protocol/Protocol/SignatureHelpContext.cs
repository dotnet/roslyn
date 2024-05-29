// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing additional information about the context in which a signature help request is triggered.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureHelpContext">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class SignatureHelpContext
    {
        /// <summary>
        /// Gets or sets the <see cref="SignatureHelpTriggerKind"/> indicating how the signature help was triggered.
        /// </summary>
        [JsonPropertyName("triggerKind")]
        public SignatureHelpTriggerKind TriggerKind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the character that caused signature help to be triggered.
        /// This value is null when triggerKind is not TriggerCharacter.
        /// </summary>
        [JsonPropertyName("triggerCharacter")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TriggerCharacter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether signature help was already showing when it was triggered.
        /// </summary>
        [JsonPropertyName("isRetrigger")]
        public bool IsRetrigger
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the currently active <see cref="SignatureHelp"/>.
        /// </summary>
        [JsonPropertyName("activeSignatureHelp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SignatureHelp? ActiveSignatureHelp
        {
            get;
            set;
        }
    }
}