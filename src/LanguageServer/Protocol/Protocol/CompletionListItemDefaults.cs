// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents default properties associated with the entire completion list.
    /// </summary>
    internal class CompletionListItemDefaults
    {
        /// <summary>
        /// Gets or sets the default commit character set.
        /// </summary>
        [JsonPropertyName("commitCharacters")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? CommitCharacters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default edit range.
        /// </summary>
        [JsonPropertyName("editRange")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<Range, InsertReplaceRange>? EditRange
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default <see cref="InsertTextFormat"/>.
        /// </summary>
        [JsonPropertyName("insertTextFormat")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InsertTextFormat? InsertTextFormat
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default <see cref="InsertTextMode"/>.
        /// </summary>
        [JsonPropertyName("insertTextMode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InsertTextMode? InsertTextMode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default completion item data.
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
