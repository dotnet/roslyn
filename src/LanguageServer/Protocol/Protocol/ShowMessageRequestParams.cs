// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents parameter sent with window/showMessageRequest requests.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#showMessageRequestParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class ShowMessageRequestParams : ShowMessageParams
    {
        /// <summary>
        /// Gets or sets an array of <see cref="MessageActionItem"/>s to present.
        /// </summary>
        [JsonPropertyName("actions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MessageActionItem[]? Actions
        {
            get;
            set;
        }
    }
}
