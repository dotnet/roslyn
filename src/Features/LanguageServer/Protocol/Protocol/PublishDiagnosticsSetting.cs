// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the initialization setting for publish diagnostics.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#publishDiagnosticsClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class PublishDiagnosticsSetting
    {
        /// <summary>
        /// Gets or sets a value indicating whether gets or sets the <see cref="TagSupport"/> capabilities.
        /// </summary>
        [DataMember(Name = "tagSupport")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TagSupport? TagSupport
        {
            get;
            set;
        }
    }
}
