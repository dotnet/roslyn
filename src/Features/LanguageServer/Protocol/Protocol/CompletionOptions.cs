// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents completion capabilities.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class CompletionOptions : IWorkDoneProgressOptions
    {
        /// <summary>
        /// Gets or sets the trigger characters.
        /// </summary>
        [JsonPropertyName("triggerCharacters")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? TriggerCharacters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating all the possible commit characters associated with the language server.
        /// </summary>
        [JsonPropertyName("allCommitCharacters")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? AllCommitCharacters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether server provides completion item resolve capabilities.
        /// </summary>
        [JsonPropertyName("resolveProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool ResolveProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether work done progress is supported.
        /// </summary>
        [JsonPropertyName("workDoneProgress")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool WorkDoneProgress { get; init; }

        /// <summary>
        /// Gets or sets completion item setting.
        /// </summary>
        [JsonPropertyName("completionItem")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionItemOptions? CompletionItemOptions
        {
            get;
            set;
        }
    }
}
