// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a response from a semantic tokens Document provider Edits request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensDelta">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class SemanticTokensDelta
    {
        /// <summary>
        /// Gets or sets the Id for the client's new version after applying all
        /// edits to their current semantic tokens data.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("resultId")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? ResultId { get; set; }

        /// <summary>
        /// Gets or sets an array of edits to apply to a previous response from a
        /// semantic tokens Document provider.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("edits"), System.Text.Json.Serialization.JsonRequired]
        public SemanticTokensEdit[] Edits { get; set; }
    }
}
