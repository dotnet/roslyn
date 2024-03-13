// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents default properties associated with the entire completion list.
    /// </summary>
    [DataContract]
    internal class CompletionListItemDefaults
    {
        /// <summary>
        /// Gets or sets the default commit character set.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("commitCharacters")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string[]? CommitCharacters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default edit range.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("editRange")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public SumType<Range, InsertReplaceRange>? EditRange
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default <see cref="InsertTextFormat"/>.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("insertTextFormat")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public InsertTextFormat? InsertTextFormat
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default <see cref="InsertTextMode"/>.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("insertTextMode")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public InsertTextMode? InsertTextMode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default completion item data.
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
