﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the parameters sent from a server to a client for the workspace/applyEdit request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#applyWorkspaceEditParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class ApplyWorkspaceEditParams
    {
        /// <summary>
        /// Gets or sets the label associated with this edit.
        /// </summary>
        [DataMember(Name = "label")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Label
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the edit to be applied to the workspace.
        /// </summary>
        [DataMember(Name = "edit")]
        public WorkspaceEdit Edit
        {
            get;
            set;
        }
    }
}
