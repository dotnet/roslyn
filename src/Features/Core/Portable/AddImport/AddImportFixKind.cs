// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.AddImport
{
    internal enum AddImportFixKind
    {
        /// <summary>
        /// Adding a project reference.
        /// </summary>
        ProjectSymbol,

        /// <summary>
        /// Adding an assembly reference.
        /// </summary>
        MetadataSymbol,

        /// <summary>
        /// Adding a package reference.
        /// </summary>
        PackageSymbol,

        /// <summary>
        /// Adding a framework reference assembly reference.
        /// </summary>
        ReferenceAssemblySymbol,
    }
}
