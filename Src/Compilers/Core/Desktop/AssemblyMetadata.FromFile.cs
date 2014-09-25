// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static partial class MetadataFileFactory // TODO: split to AssemblyMetadataFactory and ModuleMetadataFactory
    {
        /// <summary>
        /// Finds all modules of an assembly on a specified path and builds an instance of <see cref="AssemblyMetadata"/> that represents them.
        /// </summary>
        /// <param name="fullPath">The full path to the assembly on disk.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fullPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="fullPath"/> is not an absolute path.</exception>
        /// <exception cref="IOException">Error reading file <paramref name="fullPath"/>. See <see cref="Exception.InnerException"/> for details.</exception>
        public static AssemblyMetadata CreateAssembly(string fullPath)
        {
            CompilerPathUtilities.RequireAbsolutePath(fullPath, nameof(fullPath));

            return new AssemblyMetadata(CreateModule(fullPath), moduleName => CreateModuleFromFile(fullPath, moduleName));
        }

        /// <exception cref="IOException">Error reading file <paramref name="fullPath"/>. See <see cref="Exception.InnerException"/> for details.</exception>
        private static ModuleMetadata CreateModuleFromFile(string fullPath, string moduleName)
        {
            return CreateModule(PathUtilities.CombineAbsoluteAndRelativePaths(Path.GetDirectoryName(fullPath), moduleName));
        }
    }
}
