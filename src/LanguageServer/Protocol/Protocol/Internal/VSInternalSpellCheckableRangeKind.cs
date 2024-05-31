// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Enum to represent the spell checkable region kinds.
    /// </summary>
    internal enum VSInternalSpellCheckableRangeKind
    {
        /// <summary>
        /// Represents a span of a string.
        /// </summary>
        String = 0,

        /// <summary>
        /// Represents a span of a comment.
        /// </summary>
        Comment = 1,

        /// <summary>
        /// Represents a span of an identifier declaration.
        /// </summary>
        Identifier = 2,
    }
}
