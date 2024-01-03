// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents the parameter that's sent with the 'workspace/symbol' request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspaceSymbolParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class WorkspaceSymbolParams : IPartialResultParams<SymbolInformation[]>
    {
        /// <summary>
        /// Gets or sets the query (a non-empty string).
        /// </summary>
        [DataMember(Name = "query")]
        public string Query
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value of the Progress instance.
        /// </summary>
        [DataMember(Name = Methods.PartialResultTokenName)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IProgress<SymbolInformation[]>? PartialResultToken
        {
            get;
            set;
        }
    }
}
