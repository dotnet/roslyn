// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing settings for inlay hint support.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlayHintClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class InlayHintSetting : DynamicRegistrationSetting
    {
        /// <summary>
        /// Gets or sets a value indicating whether the client supports
        /// resolving lazily on an inlay hint.
        /// </summary>
        [JsonPropertyName("resolveSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InlayHintResolveSupportSetting? ResolveSupport
        {
            get;
            set;
        }
    }
}
