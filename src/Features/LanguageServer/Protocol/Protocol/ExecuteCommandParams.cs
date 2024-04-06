// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the parameters sent from client to server for the workspace/executeCommand request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#executeCommandParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class ExecuteCommandParams
    {
        /// <summary>
        /// Gets or sets the command identifier associated with the command handler.
        /// </summary>
        [DataMember(Name = "command")]
        public string Command
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the arguments that the command should be invoked with.
        /// </summary>
        [DataMember(Name = "arguments")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object[]? Arguments
        {
            get;
            set;
        }
    }
}
