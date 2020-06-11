// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Rename
{
    public enum RenameEntityKind
    {
        /// <summary>
        /// mentions that the result is for the base symbol of the rename
        /// </summary>
        BaseSymbol = 0,

        /// <summary>
        /// mentions that the result is for the overloaded symbols of the rename
        /// </summary>
        OverloadedSymbols = 1
    }
}
