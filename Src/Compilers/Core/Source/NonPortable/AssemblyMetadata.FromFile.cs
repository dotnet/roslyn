// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static partial class MetadataFileFactory
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
            CompilerPathUtilities.RequireAbsolutePath(fullPath, "fullPath");

            return new AssemblyMetadata(CreateModulesFromFile(fullPath));
        }

        private static ImmutableArray<ModuleMetadata> CreateModulesFromFile(string fullPath)
        {
            ArrayBuilder<ModuleMetadata> moduleBuilder = null;

            // if the file isn't an assembly manifest module an error will be reported later
            ModuleMetadata manifestModule = CreateModule(fullPath);

            string assemblyDir = null;
            foreach (string moduleName in manifestModule.GetModuleNames())
            {
                if (moduleBuilder == null)
                {
                    moduleBuilder = ArrayBuilder<ModuleMetadata>.GetInstance();
                    moduleBuilder.Add(manifestModule);
                    assemblyDir = Path.GetDirectoryName(fullPath);
                }

                var module = CreateModule(PathUtilities.CombineAbsoluteAndRelativePaths(assemblyDir, moduleName));
                moduleBuilder.Add(module);
            }

            return (moduleBuilder != null) ? moduleBuilder.ToImmutableAndFree() : ImmutableArray.Create(manifestModule);
        }
    }
}
