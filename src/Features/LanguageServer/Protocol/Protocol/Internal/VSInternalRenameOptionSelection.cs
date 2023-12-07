// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the user configuration (as defined in <see cref="VSInternalRenameOptionSupport"/>) for a rename request.
    /// </summary>
    [DataContract]
    internal class VSInternalRenameOptionSelection
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
        /// Gets or sets a value indicating whether the user selected the option.
        /// </summary>
        [DataMember(Name = "_vs_value")]
        [JsonProperty(Required = Required.Always)]
        public bool Value
        {
            get;
            set;
        }
    }
}
