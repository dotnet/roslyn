// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
