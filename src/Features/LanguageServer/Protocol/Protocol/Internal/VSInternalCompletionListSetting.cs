// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents initialization setting for completion list.
    /// </summary>
    [DataContract]
    internal class VSInternalCompletionListSetting
    {
        /// <summary>
        /// Gets or sets a value indicating whether completion lists can have Data bags. These data bags get propagated
        /// onto underlying completion items unless they have their own data bags.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_data")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public bool Data
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether completion lists can have VSCommitCharacters. These commit characters get propagated
        /// onto underlying valid completion items unless they have their own commit characters.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_commitCharacters")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public bool CommitCharacters
        {
            get;
            set;
        }
    }
}
