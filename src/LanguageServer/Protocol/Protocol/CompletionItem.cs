// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

/// <summary>
/// Class which represents an IntelliSense completion item.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionItem">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class CompletionItem
{
    /// <summary>
    /// The label of this completion item.
    /// <para>
    /// The label property is also by default the text that
    /// is inserted when selecting this completion.
    /// </para>
    /// <para>
    /// If label details are provided the label itself should
    /// be an unqualified name of the completion item.
    /// </para>
    /// </summary>
    [JsonPropertyName("label")]
    [JsonRequired]
    public string Label
    {
        get;
        set;
    }

    /// <summary>
    /// Additional details for the label.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("labelDetails")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CompletionItemLabelDetails? LabelDetails
    {
        get;
        set;
    }

    /// <summary>
    /// The kind of this completion item. Based on the kind
    /// an icon is chosen by the editor.
    /// </summary>
    [JsonPropertyName("kind")]
    [SuppressMessage("Microsoft.StyleCop.CSharp.LayoutRules", "SA1513:ClosingCurlyBracketMustBeFollowedByBlankLine", Justification = "There are no issues with this code")]
    [SuppressMessage("Microsoft.StyleCop.CSharp.LayoutRules", "SA1500:BracesForMultiLineStatementsShouldNotShareLine", Justification = "There are no issues with this code")]
    [DefaultValue(CompletionItemKind.None)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public CompletionItemKind Kind
    {
        get;
        set;
    } = CompletionItemKind.None;

    /// <summary>
    /// Tags for this completion item, which tweak the rendering of the item.
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public CompletionItemTag[]? Tags
    {
        get;
        set;
    }

    /// <summary>
    /// A human-readable string with additional information
    /// about this item, like type or symbol information
    /// </summary>
    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail
    {
        get;
        set;
    }

    /// <summary>
    ///  A human-readable string that represents a documentation comment
    /// </summary>
    [JsonPropertyName("documentation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public SumType<string, MarkupContent>? Documentation
    {
        get;
        set;
    }

    /// <summary>
    /// Indicates whether this item is deprecated.
    /// </summary>
    [Obsolete("Use Tags instead if supported")]
    [JsonPropertyName("deprecated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? Deprecated { get; set; }

    /// <summary>
    /// Select this item when showing.
    /// <para>
    /// Note that only one completion item can be selected and that the
    /// tool / client decides which item that is. The rule is that the *first*
    /// item of those that match best is selected
    /// </para>
    /// </summary>
    [JsonPropertyName("preselect")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Preselect
    {
        get;
        set;
    }

    /// <summary>
    /// A string that should be used when comparing this item
    /// with other items. When omitted the label is used
    /// as the sort text for this item.
    /// </summary>
    [JsonPropertyName("sortText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SortText
    {
        get;
        set;
    }

    /// <summary>
    /// A string that should be used when filtering a set of
    /// completion items. When omitted the label is used as the
    /// filter text for this item.
    /// </summary>
    [JsonPropertyName("filterText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FilterText
    {
        get;
        set;
    }

    /// <summary>
    /// A string that should be inserted into a document when selecting
    /// this completion. When omitted the label is used as the insert text
    /// for this item.
    /// <para>
    /// The <see cref="InsertText"/> is subject to interpretation by the client side,
    /// so it is recommended to use <see cref="TextEdit"/> instead which avoids
    /// client side interpretation.
    /// </para>
    /// <para>
    /// For example in VS Code when code complete is requested for
    /// <c>con&lt;cursor position></c> and an item with <see cref="InsertText"/>
    /// <c>console</c> is selected, it will only insert <c>sole</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("insertText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InsertText
    {
        get;
        set;
    }

    /// <summary>
    /// The format of the insert text. If omitted, defaults to <see cref="InsertTextFormat.Plaintext"/>
    /// <para>
    /// The format applies to both <see cref="InsertText"/> and the
    /// <see cref="TextEdit.NewText"/> property on <see cref="TextEdit"/>.
    /// </para>
    /// <para>
    /// Note that this does not apply to <see cref="AdditionalTextEdits" />
    /// </para>
    /// </summary>
    [JsonPropertyName("insertTextFormat")]
    [SuppressMessage("Microsoft.StyleCop.CSharp.LayoutRules", "SA1513:ClosingCurlyBracketMustBeFollowedByBlankLine", Justification = "There are no issues with this code")]
    [SuppressMessage("Microsoft.StyleCop.CSharp.LayoutRules", "SA1500:BracesForMultiLineStatementsShouldNotShareLine", Justification = "There are no issues with this code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DefaultValue(InsertTextFormat.Plaintext)]
    public InsertTextFormat InsertTextFormat
    {
        get;
        set;
    } = InsertTextFormat.Plaintext;

    /// <summary>
    /// How whitespace and indentation is handled during completion
    /// item insertion. If not provided the client's default value depends on
    /// the <see cref="CompletionSetting.InsertTextMode"/> client capability.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonPropertyName("insertTextMode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public InsertTextMode? InsertTextMode { get; init; }

    /// <summary>
    /// An edit which is applied to a document when selecting this completion.
    /// When an edit is provided the value of <see cref="InsertText"/> is ignored.
    /// <para>
    /// The <see cref="TextEdit.Range"/> must be single-line and must contain the position at which completion was requested.
    /// </para>
    /// <para>
    /// Most editors support two different commit operations: insert completion text,
    /// or replace existing text with completion text. This cannot usually be predetermined
    /// by a server, so it can report both ranges using <see cref="InsertReplaceEdit"/>
    /// if the client signals support via the
    /// <see cref="CompletionItemSetting.InsertReplaceSupport"/> capability.
    /// </para>
    /// <para>
    /// The <see cref="InsertReplaceEdit.Insert"/>
    /// must be a prefix of the <see cref="InsertReplaceEdit.Replace"/> i.e. same start
    /// position and contained within it. They must be single-line and must contain the
    /// position at which completion was requested.
    /// </para>
    /// </summary>
    [JsonPropertyName("textEdit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SumType<TextEdit, InsertReplaceEdit>? TextEdit
    {
        get;
        set;
    }

    /// <summary>
    /// The edit text used if the completion item is part of a
    /// <see cref="CompletionList"/> that defines a default <see cref="CompletionListItemDefaults.EditRange"/>.
    /// <para>
    /// Clients will only honor this property if they opt into completion list
    /// item defaults using the capability <see cref="CompletionListSetting.ItemDefaults"/>.
    /// </para>
    /// <para>
    /// If not provided, the <see cref="Label"/> is used.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("textEditText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TextEditText
    {
        get;
        set;
    }

    /// <summary>
    /// An optional array of additional text edits that are applied when
    /// selecting this completion.
    /// <para>
    /// Edits must not overlap (including the same
    /// insert position) with the main edit nor with themselves.
    /// </para>
    /// <para>
    /// Additional text edits should be used to change text unrelated to the
    /// current cursor position (for example adding an import statement at the
    /// top of the file).
    /// </para>
    /// </summary>
    [JsonPropertyName("additionalTextEdits")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TextEdit[]? AdditionalTextEdits
    {
        get;
        set;
    }

    /// <summary>
    /// An optional set of characters that will commit this item if typed while this completion is active.
    /// The completion will be committed before inserting the typed character.
    /// <para>
    /// </para>
    /// If present, this will override <see cref="CompletionOptions.AllCommitCharacters"/>.
    /// If absent, <see cref="CompletionOptions.AllCommitCharacters"/> will be used instead.
    /// </summary>
    [JsonPropertyName("commitCharacters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? CommitCharacters
    {
        get;
        set;
    }

    /// <summary>
    /// An optional command that is executed after inserting this completion.
    /// <para>
    /// Note that additional modifications to the current document should instead be
    /// described with <see cref="AdditionalTextEdits"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This feature is not supported in VS.
    /// </remarks>
    [JsonPropertyName("command")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Command? Command
    {
        get;
        set;
    }

    /// <summary>
    /// A data field that is preserved on a completion item between a completion
    /// request and and a completion resolve request.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data
    {
        get;
        set;
    }
}
