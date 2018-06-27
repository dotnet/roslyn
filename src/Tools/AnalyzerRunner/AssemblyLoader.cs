// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.CodeAnalysis;

namespace AnalyzerRunner
{
    internal class AssemblyLoader : IAnalyzerAssemblyLoader
    {
        public static AssemblyLoader Instance = new AssemblyLoader();

        public void AddDependencyLocation(string fullPath)
        {
        }

        public Assembly LoadFromPath(string fullPath)
        {
            return Assembly.LoadFrom(fullPath);
        }
    }
}
