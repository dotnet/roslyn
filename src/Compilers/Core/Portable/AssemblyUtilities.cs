// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

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
    }
}
