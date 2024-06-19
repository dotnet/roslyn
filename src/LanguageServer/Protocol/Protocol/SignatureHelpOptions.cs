// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the options for signature help support.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureHelpOptions">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class SignatureHelpOptions : IWorkDoneProgressOptions
    {
        /// <summary>
        /// Gets or sets the characters that trigger signature help automatically.
        /// </summary>
        [JsonPropertyName("triggerCharacters")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? TriggerCharacters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the characters that re-trigger signature help
        /// when signature help is already showing.
        /// </summary>
        /// <remarks>Since LSP 3.15</remarks>
        [JsonPropertyName("retriggerCharacters")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? RetriggerCharacters
        {
            get;
            set;
        }

        /// <inheritdoc/>
        [JsonPropertyName("workDoneProgress")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool WorkDoneProgress { get; init; }
    }
}
