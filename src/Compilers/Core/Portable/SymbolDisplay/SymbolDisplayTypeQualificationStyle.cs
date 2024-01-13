// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies how much qualification is used in symbol descriptions.
    /// </summary>
    public enum SymbolDisplayTypeQualificationStyle
    {
        /// <summary>
        /// Shows only the name of the symbol.
        /// </summary>
        NameOnly = 0,

        /// <summary>
        /// Shows the name of the symbol and the names of all containing types.
        /// </summary>
        NameAndContainingTypes = 1,

        /// <summary>
        /// Shows the name of the symbol the names of all containing types and namespaces.
        /// </summary>
        NameAndContainingTypesAndNamespaces = 2,
    }
}
