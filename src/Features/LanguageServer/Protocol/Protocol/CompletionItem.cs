// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents an IntelliSense completion item.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionItem">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class CompletionItem
    {
        /// <summary>
        /// Gets or sets the label value, i.e. display text to users.
        /// </summary>
        [JsonPropertyName("label")]
        [JsonRequired]
        public string Label
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets additional details for the label.
        /// </summary>
        [JsonPropertyName("labelDetails")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CompletionItemLabelDetails? LabelDetails
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the completion kind.
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
        /// Tags for this completion item.
        /// </summary>
        [JsonPropertyName("tags")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public CompletionItemTag[]? Tags
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the completion detail.
        /// </summary>
        [JsonPropertyName("detail")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the documentation comment.
        /// </summary>
        [JsonPropertyName("documentation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public SumType<string, MarkupContent>? Documentation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this should be the selected item when showing.
        /// </summary>
        [JsonPropertyName("preselect")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Preselect
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the custom sort text.
        /// </summary>
        [JsonPropertyName("sortText")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SortText
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the custom filter text.
        /// </summary>
        [JsonPropertyName("filterText")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FilterText
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the insert text.
        /// </summary>
        [JsonPropertyName("insertText")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? InsertText
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the insert text format.
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
        /// Gets or sets the text edit.
        /// </summary>
        [JsonPropertyName("textEdit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<TextEdit, InsertReplaceEdit>? TextEdit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the text edit text.
        /// </summary>
        [JsonPropertyName("textEditText")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TextEditText
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets any additional text edits.
        /// </summary>
        /// <remarks>
        /// Additional text edits must not interfere with the main text edit.
        /// </remarks>
        [JsonPropertyName("additionalTextEdits")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TextEdit[]? AdditionalTextEdits
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the set of characters that will commit completion when this <see cref="CompletionItem" /> is selected
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
        /// Gets or sets any optional command that will be executed after completion item insertion.
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
        /// Gets or sets any additional data that links the unresolve completion item and the resolved completion item.
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
