﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents initialization settings for workspace edit.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspaceEditClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class WorkspaceEditSetting
    {
        /// <summary>
        /// Gets or sets a value indicating whether document changes event is supported.
        /// </summary>
        [DataMember(Name = "documentChanges")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool DocumentChanges
        {
            get;
            set;
        }

        /// <summary>
        /// GEts or sets the resource operations the client supports.
        /// </summary>
        [DataMember(Name = "resourceOperations")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ResourceOperationKind[]? ResourceOperations
        {
            get;
            set;
        }
    }
}
