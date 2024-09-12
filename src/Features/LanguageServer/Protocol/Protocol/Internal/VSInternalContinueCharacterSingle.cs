// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing single continue character for completion.
    /// </summary>
    [DataContract]
    internal class VSInternalContinueCharacterSingle
    {
        /// <summary>
        /// Gets the type value.
        /// </summary>
        [DataMember(Name = "_vs_type")]
        [JsonProperty(Required = Required.Always)]
        public const string Type = "singleChar";

        /// <summary>
        /// Gets or sets the completion character.
        /// </summary>
        [DataMember(Name = "_vs_char")]
        [JsonProperty(Required = Required.Always)]
        public string Character { get; set; }
    }
}
