// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing a renaming option for customizing the edit in the 'textDocument/rename' request.
    /// </summary>
    [DataContract]
    internal class VSInternalRenameOptionSupport
    {
        /// <summary>
        /// Gets or sets the name that identifies the option.
        /// </summary>
        [DataMember(Name = "_vs_name")]
        [JsonProperty(Required = Required.Always)]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the user-facing option label.
        /// </summary>
        [DataMember(Name = "_vs_label")]
        [JsonProperty(Required = Required.Always)]
        public string Label
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the option has a default value of <c>true</c>.
        /// </summary>
        [DataMember(Name = "_vs_default")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Default
        {
            get;
            set;
        }
    }
}
