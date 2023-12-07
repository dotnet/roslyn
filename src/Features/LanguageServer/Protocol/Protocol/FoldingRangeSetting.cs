// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the folding range setting for initialization.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#foldingRangeClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class FoldingRangeSetting : DynamicRegistrationSetting
    {
        /// <summary>
        /// Gets or sets the range limit for folding ranges.
        /// </summary>
        [DataMember(Name = "rangeLimit")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? RangeLimit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether if client only supports entire line folding only.
        /// </summary>
        [DataMember(Name = "lineFoldingOnly")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool LineFoldingOnly
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating the specific options for the folding range.
        /// </summary>
        [DataMember(Name = "foldingRange")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public FoldingRangeSettingOptions? FoldingRange
        {
            get;
            set;
        }
    }
}
