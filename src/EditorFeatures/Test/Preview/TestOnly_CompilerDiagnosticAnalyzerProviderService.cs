﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Preview
{
    [Export(typeof(IHostDiagnosticAnalyzerPackageProvider))]
    internal class TestOnly_CompilerDiagnosticAnalyzerProviderService : IHostDiagnosticAnalyzerPackageProvider
    {
        private readonly HostDiagnosticAnalyzerPackage _info;

        [ImportingConstructor]
        public TestOnly_CompilerDiagnosticAnalyzerProviderService()
        {
            _info = new HostDiagnosticAnalyzerPackage("Compiler", GetCompilerAnalyzerAssemblies().Distinct().ToImmutableArray());
        }

        private static IEnumerable<string> GetCompilerAnalyzerAssemblies()
        {
            var compilerAnalyzersMap = DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap();
            foreach (var analyzers in compilerAnalyzersMap.Values)
            {
                foreach (var analyzer in analyzers)
                {
                    yield return analyzer.GetType().Assembly.Location;
                }
            }
        }

        public IAnalyzerAssemblyLoader GetAnalyzerAssemblyLoader()
        {
            return FromFileLoader.Instance;
        }

        public ImmutableArray<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackages()
        {
            return ImmutableArray.Create(_info);
        }

        public class FromFileLoader : IAnalyzerAssemblyLoader
        {
            public static FromFileLoader Instance = new FromFileLoader();

            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
            {
                return Assembly.LoadFrom(fullPath);
            }
        }
    }
}
