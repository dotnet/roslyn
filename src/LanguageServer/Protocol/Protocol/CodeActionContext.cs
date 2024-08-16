// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing diagnostic information about the context of a code action
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeActionContext">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class CodeActionContext
    {
        /// <summary>
        /// Gets or sets an array of diagnostics relevant to a code action.
        /// </summary>
        [JsonPropertyName("diagnostics")]
        public Diagnostic[] Diagnostics
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an array of code action kinds to filter for.
        /// </summary>
        [JsonPropertyName("only")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CodeActionKind[]? Only
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="CodeActionTriggerKind"/> indicating how the code action was triggered..
        /// </summary>
        [JsonPropertyName("triggerKind")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CodeActionTriggerKind? TriggerKind
        {
            get;
            set;
        }
    }
}
