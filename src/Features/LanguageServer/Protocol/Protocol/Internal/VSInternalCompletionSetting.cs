// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents initialization setting for VS completion.
    /// </summary>
    internal class VSInternalCompletionSetting : CompletionSetting
    {
        /// <summary>
        /// Gets or sets completion list setting.
        /// </summary>
        [JsonPropertyName("_vs_completionList")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VSInternalCompletionListSetting? CompletionList
        {
            get;
            set;
        }
    }
}
