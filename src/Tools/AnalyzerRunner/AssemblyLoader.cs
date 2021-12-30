// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
