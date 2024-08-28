// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// The client's capabilities specific to the <see cref="CompletionItem.InsertTextMode"/> property.
    /// </summary>
    /// <remarks>Since 3.16</remarks>
    internal class InsertTextModeSupportSetting
    {
        /// <summary>
        /// The <see cref="InsertTextMode"/> values that the client supports
        /// onf the the <see cref="CompletionItem.InsertTextMode"/> property.
        /// </summary>
        [JsonPropertyName("valueSet")]
        [JsonRequired]
        public InsertTextMode[] ValueSet
        {
            get;
            set;
        }
    }
}
