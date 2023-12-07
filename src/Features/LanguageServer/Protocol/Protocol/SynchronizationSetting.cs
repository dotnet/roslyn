// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents synchronization initialization setting.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentSyncClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class SynchronizationSetting : DynamicRegistrationSetting
    {
        /// <summary>
        /// Gets or sets a value indicating whether WillSave event is supported.
        /// </summary>
        [DataMember(Name = "willSave")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool WillSave
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether WillSaveWaitUntil event is supported.
        /// </summary>
        [DataMember(Name = "willSaveWaitUntil")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool WillSaveWaitUntil
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether DidSave event is supported.
        /// </summary>
        [DataMember(Name = "didSave")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool DidSave
        {
            get;
            set;
        }
    }
}
