// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// A single inline completion item response.
///
/// See https://github.com/microsoft/vscode/blob/075ba020e8493f40dba89891b1a08453f2c067e9/src/vscode-dts/vscode.proposed.inlineCompletions.d.ts#L78.
/// </summary>
internal sealed class VSInternalInlineCompletionItem
{
    /// <summary>
    /// Gets or sets the text to replace the range with.
    /// </summary>
    [JsonPropertyName("_vs_text")]
    [JsonRequired]
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the range to replace.
    ///
    /// See https://github.com/microsoft/vscode/blob/075ba020e8493f40dba89891b1a08453f2c067e9/src/vscode-dts/vscode.proposed.inlineCompletions.d.ts#L94.
    /// </summary>
    [JsonPropertyName("_vs_range")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Range? Range { get; set; }

    /// <summary>
    /// Gets or sets the command that is executed after inserting this completion item.
    /// </summary>
    [JsonPropertyName("_vs_command")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Command? Command { get; set; }

    /// <summary>
    /// Gets or sets the format of the insert text.
    /// </summary>
    [JsonPropertyName("_vs_insertTextFormat")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public InsertTextFormat? TextFormat { get; set; } = InsertTextFormat.Plaintext;
}
