﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the options for code lens support.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeLensOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class CodeLensOptions : IWorkDoneProgressOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not the code lens support has a resolve provider.
        /// </summary>
        [DataMember(Name = "resolveProvider")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ResolveProvider
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
