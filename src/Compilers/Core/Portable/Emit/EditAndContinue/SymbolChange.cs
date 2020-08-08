// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
