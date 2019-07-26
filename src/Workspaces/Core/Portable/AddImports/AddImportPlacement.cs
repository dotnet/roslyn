// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.AddImports
{
    /// <summary>
    /// Specifies the desired placement of added imports.
    /// </summary>
    internal enum AddImportPlacement
    {
        /// <summary>
        /// Place imports inside the namespace definition.
        /// </summary>
        InsideNamespace,

        /// <summary>
        /// Place imports outside the namespace definition.
        /// </summary>
        OutsideNamespace
    }
}
