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
        [DataMember(Name = "_vs_data")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool Data
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether completion lists can have VSCommitCharacters. These commit characters get propagated
        /// onto underlying valid completion items unless they have their own commit characters.
        /// </summary>
        [DataMember(Name = "_vs_commitCharacters")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool CommitCharacters
        {
            get;
            set;
        }
    }
}
