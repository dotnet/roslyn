// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UnitTests;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Preview
{
    [Export(typeof(IWorkspaceDiagnosticAnalyzerProviderService))]
    internal class TestOnly_CompilerDiagnosticAnalyzerProviderService : IWorkspaceDiagnosticAnalyzerProviderService
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

        public IEnumerable<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackages()
        {
            yield return _info;
        }
    }
}
