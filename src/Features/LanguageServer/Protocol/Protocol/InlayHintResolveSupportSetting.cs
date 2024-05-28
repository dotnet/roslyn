// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing settings for inlayHint/resolve support.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlayHintClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class InlayHintResolveSupportSetting
    {
        /// <summary>
        /// Gets or sets a value indicating the properties that a client can resolve lazily.
        /// </summary>
        [JsonPropertyName("properties")]
        public string[] Properties
        {
            get;
            set;
        }
    }
}
