// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Provides value for <see cref="VSInternalCompletionContext.InvokeKind"/> which specifies
    /// how completion was invoked.
    /// </summary>
    internal enum VSInternalCompletionInvokeKind
    {
        /// <summary>
        /// Completion was triggered by explicit user's gesture (e.g. Ctrl+Space, Ctr+J) or via API.
        /// </summary>
        Explicit = 0,

        /// <summary>
        /// Completion was triggered by typing an identifier.
        /// </summary>
        Typing = 1,

        /// <summary>
        /// Completion was triggered by deletion (e.g. Backspace or Delete keys).
        /// </summary>
        Deletion = 2,
    }
}
