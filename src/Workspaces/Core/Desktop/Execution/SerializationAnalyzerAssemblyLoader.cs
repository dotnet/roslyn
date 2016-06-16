// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Execution
{
    internal class SerializationAnalyzerAssemblyLoader : AbstractAnalyzerAssemblyLoader
    {
        // this information never get deleted since assembly once loaded can't be unloaded
        private readonly ConcurrentDictionary<string, string> _map =
            new ConcurrentDictionary<string, string>(concurrencyLevel: 2, capacity: 10, comparer: StringComparer.OrdinalIgnoreCase);

        public SerializationAnalyzerAssemblyLoader()
        {
        }

        public void AddPath(string displayPath, string assemblyPath)
        {
            _map[displayPath] = assemblyPath;
        }

        protected override Assembly LoadCore(string fullPath)
        {
            // Use the fallback loader if we fail to load the assembly in the default context.
            return Assembly.LoadFrom(GetAssemblyPath(fullPath));
        }

        private string GetAssemblyPath(string fullPath)
        {
            string assemblyPath;
            if (_map.TryGetValue(fullPath, out assemblyPath))
            {
                return assemblyPath;
            }

            return fullPath;
        }
    }
}
