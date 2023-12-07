// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class used to extend <see cref="FoldingRangeSetting" /> to add internal capabilities.
    /// </summary>
    internal class VSFoldingRangeSetting : FoldingRangeSetting
    {
        /// <summary>
        /// Gets or sets a value indicating whether if client only supports entire line folding only.
        /// </summary>
        [DataMember(Name = "_vs_refreshSupport")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool RefreshSupport
        {
            get;
            set;
        }
    }
}
