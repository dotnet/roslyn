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
        [DataMember(Name = "commitCharacters")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[]? CommitCharacters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default edit range.
        /// </summary>
        [DataMember(Name = "editRange")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<Range, InsertReplaceRange>? EditRange
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default <see cref="InsertTextFormat"/>.
        /// </summary>
        [DataMember(Name = "insertTextFormat")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public InsertTextFormat? InsertTextFormat
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default <see cref="InsertTextMode"/>.
        /// </summary>
        [DataMember(Name = "insertTextMode")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public InsertTextMode? InsertTextMode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default completion item data.
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
