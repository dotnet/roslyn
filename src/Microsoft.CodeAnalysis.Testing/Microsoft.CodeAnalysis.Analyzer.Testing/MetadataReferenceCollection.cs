// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Testing
{
    public class MetadataReferenceCollection : List<MetadataReference>
    {
        private static readonly ConcurrentDictionary<string, MetadataReference> s_referencesFromFiles =
            new ConcurrentDictionary<string, MetadataReference>();

        public void Add(Assembly assembly)
        {
            Add(GetOrCreateReference(assembly.Location));
        }

        public void Add(string path)
        {
            Add(GetOrCreateReference(path));
        }

        private static MetadataReference GetOrCreateReference(string path)
        {
            return s_referencesFromFiles.GetOrAdd(path, p => MetadataReferences.CreateReferenceFromFile(p));
        }
    }
}
