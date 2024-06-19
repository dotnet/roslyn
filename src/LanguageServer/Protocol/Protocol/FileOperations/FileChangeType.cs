// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// File event type enum.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#fileChangeType">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal enum FileChangeType
    {
        /// <summary>
        /// File was created.
        /// </summary>
        Created = 1,

        /// <summary>
        /// File was changed.
        /// </summary>
        Changed = 2,

        /// <summary>
        /// File was deleted.
        /// </summary>
        Deleted = 3,
    }
}
