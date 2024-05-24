// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing a reference to a command
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#command">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class Command
    {
        /// <summary>
        /// Gets or sets the title of the command.
        /// </summary>
        [JsonPropertyName("title")]
        [JsonRequired]
        public string Title
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the identifier associated with the command.
        /// </summary>
        [JsonPropertyName("command")]
        [JsonRequired]
        public string CommandIdentifier
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the arguments that the command should be invoked with.
        /// </summary>
        [JsonPropertyName("arguments")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object[]? Arguments
        {
            get;
            set;
        }
    }
}
