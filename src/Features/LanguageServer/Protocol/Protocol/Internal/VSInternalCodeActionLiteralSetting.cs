// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing support for code action literals.
    /// </summary>
    [DataContract]
    internal class VSInternalCodeActionLiteralSetting : CodeActionLiteralSetting
    {
        /// <summary>
        /// Gets or sets a value indicating what code action default groups are supported.
        /// </summary>
        [DataMember(Name = "_vs_codeActionGroup")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalCodeActionGroupSetting? CodeActionGroup
        {
            get;
            set;
        }
    }
}
