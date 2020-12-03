// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    public class TestAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        public static readonly IAnalyzerAssemblyLoader LoadFromFile =
            new TestAnalyzerAssemblyLoader();

        public static readonly IAnalyzerAssemblyLoader LoadNotImplemented =
            new TestAnalyzerAssemblyLoader(loadFromPath: _ => throw new NotImplementedException());

        private readonly Action<string>? _addDependencyLocation;
        private readonly Func<string, Assembly>? _loadFromPath;

        public TestAnalyzerAssemblyLoader(Action<string>? addDependencyLocation = null, Func<string, Assembly>? loadFromPath = null)
        {
            _addDependencyLocation = addDependencyLocation;
            _loadFromPath = loadFromPath;
        }

        public void AddDependencyLocation(string fullPath)
            => _addDependencyLocation?.Invoke(fullPath);

        public Assembly LoadFromPath(string fullPath)
            => (_loadFromPath != null) ? _loadFromPath(fullPath) : Assembly.LoadFrom(fullPath);
    }
}
