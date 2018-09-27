// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.CSharp;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics
{
    [Export(typeof(IWorkspaceDiagnosticAnalyzerProviderService))]
    internal class CSharpCompilerDiagnosticAnalyzerProviderService : IWorkspaceDiagnosticAnalyzerProviderService
    {
        private readonly HostDiagnosticAnalyzerPackage _info;

        public CSharpCompilerDiagnosticAnalyzerProviderService()
        {
            _info = new HostDiagnosticAnalyzerPackage("CSharpWorkspace", GetCompilerAnalyzerAssemblies().ToImmutableArray());
        }

        private static IEnumerable<string> GetCompilerAnalyzerAssemblies()
        {
            yield return typeof(CSharpCompilerDiagnosticAnalyzer).Assembly.Location;
        }

        public IAnalyzerAssemblyLoader GetAnalyzerAssemblyLoader()
        {
            return FromFileLoader.Instance;
        }

        public IEnumerable<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackages()
        {
            yield return _info;
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
