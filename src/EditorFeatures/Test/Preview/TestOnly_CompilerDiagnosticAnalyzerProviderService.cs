// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Preview
{
    [Export(typeof(IHostDiagnosticAnalyzerPackageProvider))]
    internal class TestOnly_CompilerDiagnosticAnalyzerProviderService : IHostDiagnosticAnalyzerPackageProvider
    {
        private readonly HostDiagnosticAnalyzerPackage _info;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestOnly_CompilerDiagnosticAnalyzerProviderService()
            => _info = new HostDiagnosticAnalyzerPackage("Compiler", GetCompilerAnalyzerAssemblies().Distinct().ToImmutableArray());

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
            => FromFileLoader.Instance;

        public ImmutableArray<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackages()
            => ImmutableArray.Create(_info);

        public class FromFileLoader : IAnalyzerAssemblyLoader
        {
            public static FromFileLoader Instance = new FromFileLoader();

            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
                => Assembly.LoadFrom(fullPath);
        }
    }
}
