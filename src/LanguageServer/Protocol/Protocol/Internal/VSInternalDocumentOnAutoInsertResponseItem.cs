// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

/// <summary>
/// Class representing the response of an AutoInsert response.
/// </summary>
internal sealed class VSInternalDocumentOnAutoInsertResponseItem
{
    /// <summary>
    /// Gets or sets the insert text format of the primary text edit. <see cref="TextEditFormat"/> for supported formats.
    /// </summary>
    [JsonPropertyName("_vs_textEditFormat")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DefaultValue(InsertTextFormat.Plaintext)]
    [SuppressMessage("Microsoft.StyleCop.CSharp.LayoutRules", "SA1513:ClosingCurlyBracketMustBeFollowedByBlankLine", Justification = "There are no issues with this code")]
    [SuppressMessage("Microsoft.StyleCop.CSharp.LayoutRules", "SA1500:BracesForMultiLineStatementsShouldNotShareLine", Justification = "There are no issues with this code")]
    public InsertTextFormat TextEditFormat
    {
        get;
        set;
    } = InsertTextFormat.Plaintext;

    /// <summary>
    /// Gets or sets the text edit.
    /// </summary>
    [JsonPropertyName("_vs_textEdit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TextEdit TextEdit
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets any optional command that will be executed after insertion.
    /// </summary>
    [JsonPropertyName("command")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Command? Command { get; set; }
}
