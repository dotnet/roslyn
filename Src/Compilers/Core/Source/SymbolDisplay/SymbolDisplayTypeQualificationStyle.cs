// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        NameOnly,

        /// <summary>
        /// Shows the name of the symbol and the names of all containing types.
        /// </summary>
        NameAndContainingTypes,

        /// <summary>
        /// Shows the name of the symbol the names of all containing types and namespaces.
        /// </summary>
        NameAndContainingTypesAndNamespaces,
    }
}