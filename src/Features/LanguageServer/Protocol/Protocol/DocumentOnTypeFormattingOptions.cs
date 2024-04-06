// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the options for on type formatting.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentOnTypeFormattingOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class DocumentOnTypeFormattingOptions
    {
        /// <summary>
        /// Gets or sets the first trigger character.
        /// </summary>
        [DataMember(Name = "firstTriggerCharacter")]
        public string FirstTriggerCharacter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets additional trigger characters.
        /// </summary>
        [DataMember(Name = "moreTriggerCharacter")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[]? MoreTriggerCharacter
        {
            get;
            set;
        }
    }
}