// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents initialization setting for VS completion.
    /// </summary>
    [DataContract]
    internal class VSInternalCompletionSetting : CompletionSetting
    {
        /// <summary>
        /// Gets or sets completion list setting.
        /// </summary>
        [DataMember(Name = "_vs_completionList")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalCompletionListSetting? CompletionList
        {
            get;
            set;
        }
    }
}
