// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    internal partial class VisualStudioWorkspaceDiagnosticAnalyzerProviderService : IWorkspaceDiagnosticAnalyzerProviderService
    {
        private sealed class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
        {
            private readonly IAnalyzerAssemblyLoader _fallbackLoader;

            public AnalyzerAssemblyLoader()
            {
                _fallbackLoader = new SimpleAnalyzerAssemblyLoader();
            }

            public void AddDependencyLocation(string fullPath)
            {
                _fallbackLoader.AddDependencyLocation(fullPath);
            }

            public Assembly LoadFromPath(string fullPath)
            {
                try
                {
                    // We want to load the analyzer assembly assets in default context.
                    // Use Assembly.Load instead of Assembly.LoadFrom to ensure that if the assembly is ngen'ed, then the native image gets loaded.
                    return Assembly.Load(AssemblyName.GetAssemblyName(fullPath));
                }
                catch (Exception)
                {
                    // Use the fallback loader if we fail to load the assembly in the default context.
                    return _fallbackLoader.LoadFromPath(fullPath);
                }
            }
        }
    }
}
