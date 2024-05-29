// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// A class representing a code lens command that should be shown alongside source code.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeLens">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class CodeLens
    {
        /// <summary>
        /// Gets or sets the range that the code lens applies to.
        /// </summary>
        [JsonPropertyName("range")]
        public Range Range
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the command associated with this code lens.
        /// </summary>
        [JsonPropertyName("command")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Command? Command
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the data that should be preserved between a textDocument/codeLens request and a codeLens/resolve request.
        /// </summary>
        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data
        {
            get;
            set;
        }
    }
}
