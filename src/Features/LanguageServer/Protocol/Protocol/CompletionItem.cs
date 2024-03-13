// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents an IntelliSense completion item.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionItem">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class CompletionItem
    {
        /// <summary>
        /// Gets or sets the label value, i.e. display text to users.
        /// </summary>
        [DataMember(Name = "label", IsRequired = true)]
        public string Label
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets additional details for the label.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("labelDetails")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public CompletionItemLabelDetails? LabelDetails
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the completion kind.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("kind")]
        [SuppressMessage("Microsoft.StyleCop.CSharp.LayoutRules", "SA1513:ClosingCurlyBracketMustBeFollowedByBlankLine", Justification = "There are no issues with this code")]
        [SuppressMessage("Microsoft.StyleCop.CSharp.LayoutRules", "SA1500:BracesForMultiLineStatementsShouldNotShareLine", Justification = "There are no issues with this code")]
        [DefaultValue(CompletionItemKind.None)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public CompletionItemKind Kind
        {
            get;
            set;
        } = CompletionItemKind.None;

        /// <summary>
        /// Gets or sets the completion detail.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("detail")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the documentation comment.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("documentation")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public SumType<string, MarkupContent>? Documentation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this should be the selected item when showing.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("preselect")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Preselect
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the custom sort text.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("sortText")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? SortText
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the custom filter text.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("filterText")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? FilterText
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the insert text.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("insertText")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? InsertText
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the insert text format.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("insertTextFormat")]
        [SuppressMessage("Microsoft.StyleCop.CSharp.LayoutRules", "SA1513:ClosingCurlyBracketMustBeFollowedByBlankLine", Justification = "There are no issues with this code")]
        [SuppressMessage("Microsoft.StyleCop.CSharp.LayoutRules", "SA1500:BracesForMultiLineStatementsShouldNotShareLine", Justification = "There are no issues with this code")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(InsertTextFormat.Plaintext)]
        public InsertTextFormat InsertTextFormat
        {
            get;
            set;
        } = InsertTextFormat.Plaintext;

        /// <summary>
        /// Gets or sets the text edit.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("textEdit")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<TextEdit, InsertReplaceEdit>? TextEdit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the text edit text.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("textEditText")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
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
        [System.Text.Json.Serialization.JsonPropertyName("additionalTextEdits")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
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
        [System.Text.Json.Serialization.JsonPropertyName("commitCharacters")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
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
        [System.Text.Json.Serialization.JsonPropertyName("command")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public Command? Command
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets any additional data that links the unresolve completion item and the resolved completion item.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public object? Data
        {
            get;
            set;
        }
    }
}
