// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represent an action the user performs after a window/showMessageRequest request is sent.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#messageActionItem">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class MessageActionItem
    {
        /// <summary>
        /// A short title like 'Retry', 'Open Log' etc.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title
        {
            get;
            set;
        }

        /// <summary>
        /// Additional properties which will be returned to the server in the request's response.
        /// <para>
        /// Support for this depends on the client capability <see cref="MessageActionItemClientCapabilities.AdditionalPropertiesSupport"/>.
        /// </para>
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object?> AdditionalProperties { get; set; }
    }
}
