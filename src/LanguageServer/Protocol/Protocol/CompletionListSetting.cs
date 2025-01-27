// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Client capabilities specific to <see cref="CompletionList"/>
    /// </summary>
    /// <remarks>Since 3.17</remarks>
    internal class CompletionListSetting
    {
        /// <summary>
        /// The supported property names of the <see cref="CompletionList.ItemDefaults"/> object.
        /// If omitted, no properties are supported.
        /// </summary>
        [JsonPropertyName("itemDefaults")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? ItemDefaults
        {
            get;
            set;
        }
    }
}
