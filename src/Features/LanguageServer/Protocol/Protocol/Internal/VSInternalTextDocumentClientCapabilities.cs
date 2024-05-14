// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Text document capabilities specific to Visual Studio.
    /// </summary>
    [DataContract]
    internal class VSInternalTextDocumentClientCapabilities : TextDocumentClientCapabilities
    {
        /// <summary>
        /// Gets or sets the setting which determines if on auto insert can be dynamically registered.
        /// </summary>
        [DataMember(Name = "_vs_onAutoInsert")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DynamicRegistrationSetting? OnAutoInsert
        {
            get;
            set;
        }
    }
}
