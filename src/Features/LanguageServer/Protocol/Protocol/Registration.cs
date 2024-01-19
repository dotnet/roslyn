// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the general registration information for registering for a capability.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#registration">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class Registration
    {
        /// <summary>
        /// Gets or sets the id used to register the request. This can be used to deregister later.
        /// </summary>
        [DataMember(Name = "id")]
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the method / capability to register for.
        /// </summary>
        [DataMember(Name = "method")]
        public string Method
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the options necessary for registration.
        /// </summary>
        [DataMember(Name = "registerOptions")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object? RegisterOptions
        {
            get;
            set;
        }
    }
}
