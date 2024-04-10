// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Roslyn.Text.Adornments;
    using Newtonsoft.Json;

    /// <summary>
    /// Extension class for signature help information which contains colorized label information.
    /// </summary>
    [DataContract]
    internal class VSInternalSignatureInformation : SignatureInformation
    {
        /// <summary>
        /// Gets or sets the value representing the colorized label.
        /// </summary>
        [DataMember(Name = "_vs_colorizedLabel")]
        [JsonConverter(typeof(ClassifiedTextElementConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ClassifiedTextElement? ColorizedLabel
        {
            get;
            set;
        }
    }
}
