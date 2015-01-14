// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the options for how to display the global namespace in the description of a symbol.
    /// </summary>
    /// <remarks>
    /// Any of these styles may be overridden by <see cref="SymbolDisplayTypeQualificationStyle"/>.
    /// </remarks>
    public enum SymbolDisplayGlobalNamespaceStyle
    {
        /// <summary>
        /// Omits the global namespace, unconditionally.
        /// </summary>
        Omitted = 0,

        /// <summary>
        /// Omits the global namespace if it is being displayed as a containing symbol (i.e. not on its own).
        /// </summary>
        OmittedAsContaining = 1,

        /// <summary>
        /// Include the global namespace, unconditionally.
        /// </summary>
        Included = 2,
    }
}
