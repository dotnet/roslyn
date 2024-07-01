// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Enum which represents the various resolutions for a reference entry.
    /// </summary>
    internal enum VSInternalResolutionStatusKind
    {
        /// <summary>
        /// Entry has been processed and confirmed as a reference.
        /// </summary>
        ConfirmedAsReference,

        /// <summary>
        /// Entry has been processed and confimed as not a reference.
        /// </summary>
        ConfirmedAsNotReference,

        /// <summary>
        /// Entry has been processed but could not be confirmed.
        /// </summary>
        NotConfirmed,

        /// <summary>
        /// Entry has not been processed.
        /// </summary>
        NotProcessed,
    }
}
