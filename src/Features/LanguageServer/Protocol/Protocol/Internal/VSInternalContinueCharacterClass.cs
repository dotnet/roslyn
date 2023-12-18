// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing a unicode character class for completion continuation.
    /// </summary>
    [DataContract]
    internal class VSInternalContinueCharacterClass
    {
        /// <summary>
        /// Gets the type value.
        /// </summary>
        [DataMember(Name = "_vs_type")]
        [JsonProperty(Required = Required.Always)]
        public const string Type = "unicodeClass";

        /// <summary>
        /// Gets or sets the unicode class.
        /// </summary>
        [DataMember(Name = "_vs_unicodeClass")]
        [JsonProperty(Required = Required.Always)]
        public string UnicodeClass { get; set; }
    }
}
