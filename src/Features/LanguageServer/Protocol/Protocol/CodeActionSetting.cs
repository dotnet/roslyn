// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing settings for code action support.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeActionClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class CodeActionSetting : DynamicRegistrationSetting
    {
        /// <summary>
        /// Gets or sets a value indicating the client supports code action literals.
        /// </summary>
        [JsonPropertyName("codeActionLiteralSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CodeActionLiteralSetting? CodeActionLiteralSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the client supports resolving
        /// additional code action properties via a separate `codeAction/resolve`
        /// request.
        /// </summary>
        [JsonPropertyName("resolveSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CodeActionResolveSupportSetting? ResolveSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether code action supports the `data`
        /// property which is preserved between a `textDocument/codeAction` and a
        /// `codeAction/resolve` request.
        /// </summary>
        [JsonPropertyName("dataSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool DataSupport
        {
            get;
            set;
        }
    }
}
