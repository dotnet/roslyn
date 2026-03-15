// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// An inline completion item represents a text snippet that is proposed inline
/// to complete text that is being typed.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlineCompletionItem">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.18</remarks>
internal sealed class InlineCompletionItem
{
    /// <summary>
    /// The text to replace the range with. Must be set.
    /// <para>
    /// Is used both for the preview and the accept operation.
    /// </para>
    /// </summary>
    [JsonPropertyName("insertText")]
    [JsonRequired]
    public SumType<string, StringValue> InsertText { get; set; }

    /// <summary>
    /// A text that is used to decide if this inline completion should be shown.
    /// When <see langword="null"/> the <see cref="InlineCompletionItem.InsertText"/> is used.
    /// <para>
    /// An inline completion is shown if the text to replace is a prefix of the
    /// filter text.
    /// </para>
    /// </summary>
    [JsonPropertyName("filterText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FilterText { get; set; }

    /// <summary>
    /// The range to replace.
    /// <para>
    /// Must begin and end on the same line.
    /// </para>
    /// <para>
    /// Prefer replacements over insertions to provide a better experience when
    /// the user deletes typed text.
    /// </para>
    /// </summary>
    [JsonPropertyName("range")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Range? Range { get; set; }

    /// <summary>
    /// An optional <see cref="Protocol.Command"/> that is executed <em>after</em>
    /// inserting this completion.
    /// </summary>
    [JsonPropertyName("command")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Command? Command { get; set; }

    /// <summary>
    /// The format of the insert text. The format applies to the <see cref="InsertText"/>.
    /// If omitted defaults to <see cref="InsertTextFormat.Plaintext"/>.
    /// </summary>
    [JsonPropertyName("insertTextFormat")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public InsertTextFormat InsertTextFormat { get; set; } = InsertTextFormat.Plaintext;
}
