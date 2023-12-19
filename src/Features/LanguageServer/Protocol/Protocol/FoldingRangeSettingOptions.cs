﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the specific options for the folding range.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#foldingRangeClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class FoldingRangeSettingOptions : DynamicRegistrationSetting
    {
        /// <summary>
        /// Gets or sets a value indicating whether if client supports collapsedText on folding ranges.
        /// </summary>
        [DataMember(Name = "collapsedText")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool CollapsedText
        {
            get;
            set;
        }
    }
}
