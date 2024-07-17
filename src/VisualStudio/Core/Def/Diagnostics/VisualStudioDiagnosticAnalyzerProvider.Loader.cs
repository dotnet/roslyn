// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;

internal partial class VisualStudioDiagnosticAnalyzerProvider
{
    private sealed class Loader : IAnalyzerAssemblyLoader
    {
        private readonly IAnalyzerAssemblyLoader _fallbackLoader;

        public Loader()
        {
            _fallbackLoader = new DefaultAnalyzerAssemblyLoader();
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
