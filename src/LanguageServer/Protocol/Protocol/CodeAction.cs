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
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeAction">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class CodeAction
    {
        /// <summary>
        /// A short human readable title for this code action.
        /// </summary>
        [JsonPropertyName("title")]
        [JsonRequired]
        public string Title
        {
            get;
            set;
        }

        /// <summary>
        /// The kind of the code action, used to filter code actions.
        /// </summary>
        [JsonPropertyName("kind")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CodeActionKind? Kind
        {
            get;
            set;
        }

        /// <summary>
        /// The diagnostics that this code action resolves.
        /// </summary>
        [JsonPropertyName("diagnostics")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Diagnostic[]? Diagnostics
        {
            get;
            set;
        }

        /// <summary>
        /// Marks this as a preferred action. Preferred actions are used by the
        /// Auto Fix command and can be targeted by keybindings.
        /// <para>
        /// A quick fix should be marked preferred if it properly addresses the
        /// underlying error. A refactoring should be marked preferred if it is the
        /// most reasonable choice of actions to take.
        /// </para>
        /// </summary>
        /// <remarks>Since LSP 3.15</remarks>
        [JsonPropertyName("preferred")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsPreferred { get; init; }

        /// <summary>
        /// Marks that the code action cannot currently be applied.
        /// <para>
        /// Clients should follow the following guidelines regarding disabled code
        /// actions:
        /// <list type="bullet">
        /// <item>
        /// Disabled code actions are not shown in automatic lightbulbs code
        /// action menus.
        /// </item>
        /// <item>
        /// Disabled actions are shown as faded out in the code action menu when
        /// the user request a more specific type of code action, such as
        /// refactorings.
        /// </item>
        /// <item>
        /// If the user has a keybinding that auto applies a code action and only
        /// a disabled code actions are returned, the client should show the user
        /// an error message with <see cref="CodeActionDisabledReason.Reason"/> in the editor.
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("disabled")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CodeActionDisabledReason? Disabled { get; init; }

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
        /// A command this code action executes. If a code action
        /// provides an edit and a command, first the edit is
        /// executed and then the command.
        /// </summary>
        [JsonPropertyName("command")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Command? Command
        {
            get;
            set;
        }

        /// <summary>
        /// Data field that is preserved on a code action between a <c>textDocument/codeAction</c> request
        /// and a <c>codeAction/resolve</c> request.
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data
        {
            get;
            set;
        }
    }
}
