// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Enum representing the reason a document was saved.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentSaveReason">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal enum TextDocumentSaveReason
    {
        /// <summary>
        /// Save was manually triggered.
        /// </summary>
        Manual = 1,

        /// <summary>
        /// Save was automatic after some delay.
        /// </summary>
        AfterDelay = 2,

        /// <summary>
        /// Save was automatic after the editor lost focus.
        /// </summary>
        FocusOut = 3,
    }
}