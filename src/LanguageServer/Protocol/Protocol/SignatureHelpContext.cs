// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing additional information about the context in which a signature help request is triggered.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureHelpContext">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
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
        /// <para>
        /// This is defined when <see cref="TriggerCharacter"/> when is not
        /// <see cref="SignatureHelpTriggerKind.TriggerCharacter"/>.
        /// </para>
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
        /// <para>
        /// Retriggers occur when the signature help is already active and can be 
        /// caused by actions such as typing a trigger character, a cursor move, or 
        /// document content changes.
        /// </para>
        /// </summary>
        [JsonPropertyName("isRetrigger")]
        public bool IsRetrigger
        {
            get;
            set;
        }

        /// <summary>
        /// The currently active <see cref="SignatureHelp"/>.
        /// <para>
        /// The <see cref="ActiveSignatureHelp"/> has its <see cref="SignatureHelp.ActiveSignature"/>
        /// updated based on the user navigating through available signatures.
        /// </para>
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
