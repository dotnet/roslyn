﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the options for execute command support.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#executeCommandOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class ExecuteCommandOptions : IWorkDoneProgressOptions
    {
        /// <summary>
        /// Gets or sets the commands that are to be executed on the server.
        /// </summary>
        [DataMember(Name = "commands")]
        public string[] Commands
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether work done progress is supported.
        /// </summary>
        [DataMember(Name = "workDoneProgress")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool WorkDoneProgress
        {
            get;
            set;
        }
    }
}
