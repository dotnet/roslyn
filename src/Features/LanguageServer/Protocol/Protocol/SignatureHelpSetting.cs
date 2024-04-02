// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the signature help initialization setting.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureHelpClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class SignatureHelpSetting : DynamicRegistrationSetting
    {
        /// <summary>
        /// Gets or sets the <see cref="SignatureInformationSetting"/> information.
        /// </summary>
        [DataMember(Name = "signatureInformation")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SignatureInformationSetting? SignatureInformation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether additional context information
        /// is supported for the `textDocument/signatureHelp` request.
        /// </summary>
        [DataMember(Name = "contextSupport")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ContextSupport
        {
            get;
            set;
        }
    }
}