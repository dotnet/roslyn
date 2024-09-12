// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static partial class AssemblyUtilities
    {
        /// <summary>
        /// Given a path to an assembly, finds the paths to all of its satellite
        /// assemblies.
        /// </summary>
        /// <exception cref="IOException">If the file at <paramref name="filePath"/> does not exist or cannot be accessed.</exception>
        /// <exception cref="BadImageFormatException">If the file is not an assembly or is somehow corrupted.</exception>
        public static ImmutableArray<string> FindSatelliteAssemblies(string filePath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(filePath));

            var builder = ImmutableArray.CreateBuilder<string>();

            string? directory = Path.GetDirectoryName(filePath);
            RoslynDebug.AssertNotNull(directory);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string resourcesNameWithoutExtension = fileNameWithoutExtension + ".resources";
            string resourcesNameWithExtension = resourcesNameWithoutExtension + ".dll";

            foreach (var subDirectory in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
            {
                string satelliteAssemblyPath = Path.Combine(subDirectory, resourcesNameWithExtension);
                if (File.Exists(satelliteAssemblyPath))
                {
                    builder.Add(satelliteAssemblyPath);
                }

                satelliteAssemblyPath = Path.Combine(subDirectory, resourcesNameWithoutExtension, resourcesNameWithExtension);
                if (File.Exists(satelliteAssemblyPath))
                {
                    builder.Add(satelliteAssemblyPath);
                }
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Given a path to an assembly and a set of paths to possible dependencies,
        /// identifies which of the assembly's references are missing. May throw.
        /// </summary>
        /// <exception cref="IOException">If the files does not exist or cannot be accessed.</exception>
        /// <exception cref="BadImageFormatException">If one of the files is not an assembly or is somehow corrupted.</exception>
        public static ImmutableArray<AssemblyIdentity> IdentifyMissingDependencies(string assemblyPath, IEnumerable<string> dependencyFilePaths)
        {
            RoslynDebug.Assert(PathUtilities.IsAbsolute(assemblyPath));
            RoslynDebug.Assert(dependencyFilePaths != null);

            HashSet<AssemblyIdentity> assemblyDefinitions = new HashSet<AssemblyIdentity>();
            foreach (var potentialDependency in dependencyFilePaths)
            {
                using (var reader = new PEReader(FileUtilities.OpenRead(potentialDependency)))
                {
                    var metadataReader = reader.GetMetadataReader();
                    var assemblyDefinition = metadataReader.ReadAssemblyIdentityOrThrow();

                    assemblyDefinitions.Add(assemblyDefinition);
                }
            }

            HashSet<AssemblyIdentity> assemblyReferences = new HashSet<AssemblyIdentity>();
            using (var reader = new PEReader(FileUtilities.OpenRead(assemblyPath)))
            {
                var metadataReader = reader.GetMetadataReader();

                var references = metadataReader.GetReferencedAssembliesOrThrow();

                assemblyReferences.AddAll(references);
            }

            assemblyReferences.ExceptWith(assemblyDefinitions);

            return ImmutableArray.CreateRange(assemblyReferences);
        }

        /// <summary>
        /// Given a path to an assembly, returns the <see cref="AssemblyIdentity"/> for the assembly.
        ///  May throw.
        /// </summary>
        /// <exception cref="IOException">If the file at <paramref name="assemblyPath"/> does not exist or cannot be accessed.</exception>
        /// <exception cref="BadImageFormatException">If the file is not an assembly or is somehow corrupted.</exception>
        public static AssemblyIdentity GetAssemblyIdentity(string assemblyPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(assemblyPath));

            using (var reader = new PEReader(FileUtilities.OpenRead(assemblyPath)))
            {
                var metadataReader = reader.GetMetadataReader();
                var assemblyIdentity = metadataReader.ReadAssemblyIdentityOrThrow();

                return assemblyIdentity;
            }
        }
    }
}
