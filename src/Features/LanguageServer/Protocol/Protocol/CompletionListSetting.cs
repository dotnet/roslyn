﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents capabilites for the completion list type.
    /// </summary>
    [DataContract]
    internal class CompletionListSetting
    {
        /// <summary>
        /// Gets or sets a value containing the supported property names of the <see cref="CompletionList.ItemDefaults"/> object.
        /// If omitted, no properties are supported.
        /// </summary>
        [DataMember(Name = "itemDefaults")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[]? ItemDefaults
        {
            get;
            set;
        }
    }
}
