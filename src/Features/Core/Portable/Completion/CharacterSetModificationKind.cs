// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// The kind of character set modification.
    /// </summary>
    public enum CharacterSetModificationKind
    {
        /// <summary>
        /// The rule adds new characters onto the existing set of characters.
        /// </summary>
        Add,

        /// <summary>
        /// The rule removes characters from the existing set of characters.
        /// </summary>
        Remove,

        /// <summary>
        /// The rule replaces the existing set of characters.
        /// </summary>
        Replace
    }
}
