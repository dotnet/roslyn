// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents completion capabilities.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionOptions">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class CompletionOptions : IWorkDoneProgressOptions
    {
        /// <summary>
        /// The additional characters, beyond the defaults provided by the client (typically
        /// [a-zA-Z]), that should automatically trigger a completion request.
        /// <para>
        /// For example <c>.</c> JavaScript represents the beginning of an object property
        /// or method and is thus a good candidate for triggering a completion request.
        /// </para>
        ///<para>
        /// Most tools trigger a completion request automatically without explicitly
        /// requesting it using a keyboard shortcut (e.g.Ctrl+Space). Typically they
        /// do so when the user starts to type an identifier.
        ///</para>
        ///<para>
        /// For example if the user types <c>c</c> in a JavaScript file code complete will
        /// automatically pop up present <c>console</c> besides others as a completion item.
        /// Characters that make up identifiers don't need to be listed here.
        ///</para>
        /// </summary>
        [JsonPropertyName("triggerCharacters")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? TriggerCharacters
        {
            get;
            set;
        }

        /// <summary>
        /// The list of all possible characters that commit a completion.
        /// <para>
        /// This field can be used if clients don't support individual commit characters per
        /// completion item. See client capability <see cref="CompletionItemSetting.CommitCharactersSupport"/>.
        /// </para>
        /// <para>
        /// If a server provides both <see cref="AllCommitCharacters"/> and commit characters on 
        /// an individual completion item the ones on the completion item win.
        /// </para>
        /// </summary>
        /// <remarks>Since LSP 3.2</remarks>
        [JsonPropertyName("allCommitCharacters")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? AllCommitCharacters
        {
            get;
            set;
        }

        /// <summary>
        /// The server provides support to resolve additional information for a completion item.
        /// </summary>
        [JsonPropertyName("resolveProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool ResolveProvider
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets completion item setting.
        /// </summary>
        /// <remarks>Since LSP 3.17</remarks>
        [JsonPropertyName("completionItem")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionItemOptions? CompletionItemOptions
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
    }
}
