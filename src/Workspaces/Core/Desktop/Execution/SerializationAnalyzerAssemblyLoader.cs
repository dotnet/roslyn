// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// This is assembly loader for serialized analyzer reference. 
    /// 
    /// this will record display path (<see cref="AnalyzerFileReference.FullPath"/>  returns) and 
    /// actual path (<see cref="AnalyzerFileReference.GetAssembly()"/> ) assembly needed to be loaded 
    /// 
    /// when requested, it will load from actual path.
    /// </summary>
    internal class SerializationAnalyzerAssemblyLoader : DesktopAnalyzerAssemblyLoader
    {
        // this information never get deleted since assembly once loaded can't be unloaded
        private readonly ConcurrentDictionary<string, string> _map =
            new ConcurrentDictionary<string, string>(concurrencyLevel: 2, capacity: 10, comparer: StringComparer.OrdinalIgnoreCase);

        public SerializationAnalyzerAssemblyLoader()
        {
        }

        public void AddPath(string displayPath, string assemblyPath)
        {
            AddDependencyLocation(assemblyPath);

            _map[displayPath] = assemblyPath;
        }

        protected override Assembly LoadImpl(string fullPath)
        {
            return base.LoadImpl(GetAssemblyPath(fullPath));
        }

        private string GetAssemblyPath(string fullPath)
        {
            if (_map.TryGetValue(fullPath, out var assemblyPath))
            {
                return assemblyPath;
            }

            return fullPath;
        }
    }
}
