// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the client's support for code action literals in the response of the <c>textDocument/codeAction</c> request
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeActionClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.8</remarks>
    internal class CodeActionLiteralSetting
    {
        /// <summary>
        /// Gets or sets a value indicating what code action kinds are supported.
        /// </summary>
        [JsonPropertyName("codeActionKind")]
        [JsonRequired]
        public CodeActionKindSetting CodeActionKind
        {
            get;
            set;
        }
    }
}
