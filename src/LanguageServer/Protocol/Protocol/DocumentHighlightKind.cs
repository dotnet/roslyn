// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Enum representing the different types of document highlight.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentHighlightKind">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal enum DocumentHighlightKind
    {
        /// <summary>
        /// A textual occurance.
        /// </summary>
        Text = 1,

        /// <summary>
        /// Read access of a symbol.
        /// </summary>
        Read = 2,

        /// <summary>
        /// Write access of a symbol.
        /// </summary>
        Write = 3,
    }
}