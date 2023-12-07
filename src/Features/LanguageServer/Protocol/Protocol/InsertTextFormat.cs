// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Enum representing insert text format for completion items.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#insertTextFormat">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal enum InsertTextFormat
    {
        /// <summary>
        /// Completion item insertion is plaintext.
        /// </summary>
        Plaintext = 1,

        /// <summary>
        /// Completion item insertion is snippet.
        /// </summary>
        Snippet = 2,
    }
}
