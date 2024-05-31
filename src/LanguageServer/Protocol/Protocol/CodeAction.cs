// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// A class representing a change that can be performed in code. A CodeAction must either set
    /// <see cref="CodeAction.Edit"/> or <see cref="CodeAction.Command"/>. If both are supplied,
    /// the edit will be applied first, then the command will be executed.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeAction">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class CodeAction
    {
        /// <summary>
        /// Gets or sets the human readable title for this code action.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the kind of code action this instance represents.
        /// </summary>
        [JsonPropertyName("kind")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CodeActionKind? Kind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the diagnostics that this code action resolves.
        /// </summary>
        [JsonPropertyName("diagnostics")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Diagnostic[]? Diagnostics
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the workspace edit that this code action performs.
        /// </summary>
        [JsonPropertyName("edit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public WorkspaceEdit? Edit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the command that this code action executes.
        /// </summary>
        [JsonPropertyName("command")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Command? Command
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the data that will be resend to the server if the code action is selected to be resolved.
        /// </summary>
        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data
        {
            get;
            set;
        }
    }
}
