// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal enum SymbolChange
    {
        /// <summary>
        /// No change to symbol or members.
        /// </summary>
        None = 0,

        /// <summary>
        /// No change to symbol but may contain changed symbols.
        /// </summary>
        ContainsChanges,

        /// <summary>
        /// Symbol updated.
        /// </summary>
        Updated,

        /// <summary>
        /// Symbol added.
        /// </summary>
        Added,
    }
}
