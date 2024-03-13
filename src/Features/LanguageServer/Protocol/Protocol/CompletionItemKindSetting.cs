// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents the initialization setting for completion item kind
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class CompletionItemKindSetting
    {
        /// <summary>
        /// Gets or sets the <see cref="CompletionItemKind"/> values that the client supports.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("valueSet")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public CompletionItemKind[]? ValueSet
        {
            get;
            set;
        }
    }
}