// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the options for signature help support.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureHelpOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class SignatureHelpOptions : IWorkDoneProgressOptions
    {
        /// <summary>
        /// Gets or sets the characters that trigger signature help automatically.
        /// </summary>
        [DataMember(Name = "triggerCharacters")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[]? TriggerCharacters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the characters that re-trigger signature help
        /// when signature help is already showing.
        /// </summary>
        [DataMember(Name = "retriggerCharacters")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[]? RetriggerCharacters
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether work done progress is supported.
        /// </summary>
        [DataMember(Name = "workDoneProgress")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool WorkDoneProgress { get; init; }
    }
}
