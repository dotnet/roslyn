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
        [DataMember(Name = "labelDetails")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CompletionItemLabelDetails? LabelDetails
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the completion kind.
        /// </summary>
        [DataMember(Name = "kind")]
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
        [DataMember(Name = "detail")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Detail
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the documentation comment.
        /// </summary>
        [DataMember(Name = "documentation")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public SumType<string, MarkupContent>? Documentation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this should be the selected item when showing.
        /// </summary>
        [DataMember(Name = "preselect")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Preselect
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the custom sort text.
        /// </summary>
        [DataMember(Name = "sortText")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? SortText
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the custom filter text.
        /// </summary>
        [DataMember(Name = "filterText")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? FilterText
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the insert text.
        /// </summary>
        [DataMember(Name = "insertText")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? InsertText
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the insert text format.
        /// </summary>
        [DataMember(Name = "insertTextFormat")]
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
        [DataMember(Name = "textEdit")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<TextEdit, InsertReplaceEdit>? TextEdit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the text edit text.
        /// </summary>
        [DataMember(Name = "textEditText")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
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
        [DataMember(Name = "additionalTextEdits")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
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
        [DataMember(Name = "commitCharacters")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
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
        [DataMember(Name = "command")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Command? Command
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets any additional data that links the unresolve completion item and the resolved completion item.
        /// </summary>
        [DataMember(Name = "data")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object? Data
        {
            get;
            set;
        }
    }
}
