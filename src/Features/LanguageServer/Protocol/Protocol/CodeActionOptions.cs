﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the registration options for code actions support.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeActionOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class CodeActionOptions : IWorkDoneProgressOptions
    {
        /// <summary>
        /// Gets or sets the kinds of code action that this server may return.
        /// </summary>
        /// <remarks>
        /// The list of kinds may be generic, such as `CodeActionKind.Refactor`, or the server
        /// may list out every specific kind they provide.
        /// </remarks>
        [DataMember(Name = "codeActionKinds")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CodeActionKind[]? CodeActionKinds
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

        /// <summary>
        /// Gets or sets a value indicating whether the server provides support to resolve
        /// additional information for a code action.
        /// </summary>
        [DataMember(Name = "resolveProvider")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ResolveProvider
        {
            get;
            set;
        }
    }
}
