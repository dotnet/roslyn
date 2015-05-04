// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class AssemblyUtilities
    {
        /// <summary>
        /// Given a path to an assembly, identifies files in the same directory
        /// that could satisfy the assembly's dependencies.
        /// </summary>
        /// <remarks>
        /// Dependencies are identified by simply checking the name of an assembly
        /// reference against a file name; if they match the file is considered a
        /// dependency. Other factors, such as version, culture, public key, etc., 
        /// are not considered, and so the returned collection may include items that
        /// cannot in fact satisfy the original assembly's dependencies.
        /// </remarks>
        public static ImmutableArray<string> FindAssemblySet(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            Queue<string> workList = new Queue<string>();
            HashSet<string> assemblySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            workList.Enqueue(filePath);

            while (workList.Count > 0)
            {
                string assemblyPath = workList.Dequeue();

                assemblySet.Add(assemblyPath);

                var directory = Path.GetDirectoryName(assemblyPath);

                using (var reader = new PEReader(FileUtilities.OpenRead(assemblyPath)))
                {
                    var metadataReader = reader.GetMetadataReader();
                    var assemblyReferenceHandles = metadataReader.AssemblyReferences;

                    foreach (var handle in assemblyReferenceHandles)
                    {
                        var reference = metadataReader.GetAssemblyReference(handle);
                        var referenceName = metadataReader.GetString(reference.Name);

                        string referencePath = Path.Combine(directory, referenceName + ".dll");

                        if (!assemblySet.Contains(referencePath) &&
                            File.Exists(referencePath))
                        {
                            workList.Enqueue(referencePath);
                        }
                    }
                }
            }

            return ImmutableArray.CreateRange(assemblySet);
        }

        /// <summary>
        /// Given a path to an assembly and a loaded <see cref="Assembly"/>, returns
        /// true if their MVIDs match, and false otherwise.
        /// </summary>
        public static bool MvidsMatch(string filePath, Assembly assembly)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            using (var reader = new PEReader(FileUtilities.OpenRead(filePath)))
            {
                var metadataReader = reader.GetMetadataReader();
                var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
                var fileMvid = metadataReader.GetGuid(mvidHandle);

                return fileMvid == assembly.ManifestModule.ModuleVersionId;
            }
        }

        /// <summary>
        /// Given a path to an assembly, finds the paths to all of its satellite
        /// assemblies.
        /// </summary>
        public static ImmutableArray<string> FindSatelliteAssemblies(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var builder = ImmutableArray.CreateBuilder<string>();

            string directory = Path.GetDirectoryName(filePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string resourcesNameWithoutExtension = fileNameWithoutExtension + ".resources";
            string resourcesNameWithExtension = resourcesNameWithoutExtension + ".dll";

            foreach (var subDirectory in Directory.EnumerateDirectories(directory))
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
        /// identifies which of the assembly's references are missing.
        /// </summary>
        public static ImmutableArray<AssemblyIdentity> IdentifyMissingDependencies(string assemblyPath, IEnumerable<string> assemblySet)
        {
            if (assemblyPath == null)
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            if (assemblySet == null)
            {
                throw new ArgumentNullException(nameof(assemblySet));
            }

            HashSet<AssemblyIdentity> assemblyDefinitions = new HashSet<AssemblyIdentity>();
            foreach (var potentialDependency in assemblySet)
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
        /// Given a path to an assembly, returns true if the assembly has a strong
        /// name (either a full key or a token) or false otherwise.
        /// </summary>
        public static bool HasStrongName(string assemblyPath)
        {
            if (assemblyPath == null)
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            using (var reader = new PEReader(FileUtilities.OpenRead(assemblyPath)))
            {
                var metadataReader = reader.GetMetadataReader();
                var assemblyDefinition = metadataReader.ReadAssemblyIdentityOrThrow();

                return assemblyDefinition.IsStrongName;
            }
        }
    }
}
