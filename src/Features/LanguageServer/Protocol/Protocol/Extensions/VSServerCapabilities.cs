// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// <see cref="VSServerCapabilities"/> extends <see cref="ServerCapabilities"/> allowing to provide
    /// additional capabilities supported by Visual Studio.
    /// </summary>
    [DataContract]
    internal class VSServerCapabilities : ServerCapabilities
    {
        /// <summary>
        /// Gets or sets a value indicating whether the server supports the
        /// 'textDocument/_vs_getProjectContexts' request.
        /// </summary>
        [DataMember(Name = "_vs_projectContextProvider")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ProjectContextProvider
        {
            get;
            set;
        }
    }
}