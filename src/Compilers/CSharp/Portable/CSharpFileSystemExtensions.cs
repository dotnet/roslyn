// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp
{
    public static class CSharpFileSystemExtensions
    {
        /// <summary>
        /// Emit the IL for the compilation into the specified stream.
        /// </summary>
        /// <param name="compilation">Compilation.</param>
        /// <param name="outputPath">Path of the file to which the PE image will be written.</param>
        /// <param name="pdbPath">Path of the file to which the compilation's debug info will be written.
        /// Also embedded in the output file.  Null to forego PDB generation.
        /// </param>
        /// <param name="xmlDocumentationPath">Path of the file to which the compilation's XML documentation will be written.  Null to forego XML generation.</param>
        /// <param name="win32ResourcesPath">Path of the file from which the compilation's Win32 resources will be read (in RES format).  
        /// Null to indicate that there are none.</param>
        /// <param name="manifestResources">List of the compilation's managed resources.  Null to indicate that there are none.</param>
        /// <param name="cancellationToken">To cancel the emit process.</param>
        /// <exception cref="ArgumentNullException">Compilation or path is null.</exception>
        /// <exception cref="ArgumentException">Path is empty or invalid.</exception>
        /// <exception cref="IOException">An error occurred while reading or writing a file.</exception>
        public static EmitResult Emit(
            this CSharpCompilation compilation,
            string outputPath,
            string? pdbPath = null,
            string? xmlDocumentationPath = null,
            string? win32ResourcesPath = null,
            IEnumerable<ResourceDescription>? manifestResources = null,
            CancellationToken cancellationToken = default)
        {
            return FileSystemExtensions.Emit(compilation, outputPath, pdbPath, xmlDocumentationPath, win32ResourcesPath, manifestResources, cancellationToken);
        }
    }
}
