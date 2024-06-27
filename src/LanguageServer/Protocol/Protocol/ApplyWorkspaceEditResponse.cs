// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the response sent for a workspace/applyEdit request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#applyWorkspaceEditResult">Language Server Protocol specification</see> for additional information.
    /// </summary>

    internal class ApplyWorkspaceEditResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether edits were applied or not.
        /// </summary>
        [JsonPropertyName("applied")]
        public bool Applied
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a string with textual description for why the edit was not applied.
        /// </summary>
        [JsonPropertyName("failureReason")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FailureReason
        {
            get;
            set;
        }
    }
}
