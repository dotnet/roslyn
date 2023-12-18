// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing range of characters for completion continuation.
    /// </summary>
    [DataContract]
    internal class VSInternalContinueCharacterRange
    {
        /// <summary>
        /// Gets the type value.
        /// </summary>
        [DataMember(Name = "_vs_type")]
        [JsonProperty(Required = Required.Always)]
        public const string Type = "charRange";

        /// <summary>
        /// Gets or sets the first completion character of the range.
        /// </summary>
        [DataMember(Name = "_vs_start")]
        [JsonProperty(Required = Required.Always)]
        public string Start { get; set; }

        /// <summary>
        /// Gets or sets the last completion character of the range.
        /// </summary>
        [DataMember(Name = "_vs_end")]
        [JsonProperty(Required = Required.Always)]
        public string End { get; set; }
    }
}
