// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents default values of <see cref="CompletionItem"/> properties for items
    /// is the completion list that do not provide a value for those properties.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    internal class CompletionListItemDefaults
    {
        /// <summary>
        /// A default commit character set.
        /// </summary>
        [JsonPropertyName("commitCharacters")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? CommitCharacters
        {
            get;
            set;
        }

        /// <summary>
        /// A default edit range.
        /// </summary>
        [JsonPropertyName("editRange")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<Range, InsertReplaceRange>? EditRange
        {
            get;
            set;
        }

        /// <summary>
        /// A default <see cref="InsertTextFormat"/>.
        /// </summary>
        [JsonPropertyName("insertTextFormat")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InsertTextFormat? InsertTextFormat
        {
            get;
            set;
        }

        /// <summary>
        /// A default <see cref="InsertTextMode"/>.
        /// </summary>
        [JsonPropertyName("insertTextMode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InsertTextMode? InsertTextMode
        {
            get;
            set;
        }

        /// <summary>
        /// A completion item data value.
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
