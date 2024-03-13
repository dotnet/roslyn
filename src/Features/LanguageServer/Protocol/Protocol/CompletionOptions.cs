// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents completion capabilities.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class CompletionOptions : IWorkDoneProgressOptions
    {
        /// <summary>
        /// Gets or sets the trigger characters.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("triggerCharacters")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[]? TriggerCharacters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating all the possible commit characters associated with the language server.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("allCommitCharacters")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[]? AllCommitCharacters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether server provides completion item resolve capabilities.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("resolveProvider")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ResolveProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether work done progress is supported.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("workDoneProgress")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool WorkDoneProgress { get; init; }

        /// <summary>
        /// Gets or sets completion item setting.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("completionItem")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CompletionItemOptions? CompletionItemOptions
        {
            get;
            set;
        }
    }
}
