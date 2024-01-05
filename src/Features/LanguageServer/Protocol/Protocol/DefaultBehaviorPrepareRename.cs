﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents a possible result value of the 'textDocument/prepareRename' request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_prepareRename">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class DefaultBehaviorPrepareRename
    {
        /// <summary>
        /// Gets or sets a value indicating whether the rename position is valid and the client should use its
        /// default behavior to compute the rename range.
        /// </summary>
        [DataMember(Name = "defaultBehavior")]
        [JsonProperty(Required = Required.Always)]
        public bool DefaultBehavior
        {
            get;
            set;
        }
    }
}
