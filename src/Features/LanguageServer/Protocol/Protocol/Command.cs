// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing a reference to a command
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#command">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class Command
    {
        /// <summary>
        /// Gets or sets the title of the command.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("title")]
        [JsonProperty(Required = Required.Always)]
        public string Title
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the identifier associated with the command.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("command")]
        [JsonProperty(Required = Required.Always)]
        public string CommandIdentifier
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the arguments that the command should be invoked with.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("arguments")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public object[]? Arguments
        {
            get;
            set;
        }
    }
}
