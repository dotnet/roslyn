// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
internal class HostDiagnosticAnalyzerProvider : IHostDiagnosticAnalyzerProvider
{

    private readonly ImmutableArray<(AnalyzerFileReference reference, string extensionId)> _analyzerReferences;

    public HostDiagnosticAnalyzerProvider(string? razorSourceGenerator)
    {
        if (razorSourceGenerator == null || !File.Exists(razorSourceGenerator))
        {
            _analyzerReferences = ImmutableArray<(AnalyzerFileReference reference, string extensionId)>.Empty;
        }
        else
        {
            _analyzerReferences = ImmutableArray.Create<(AnalyzerFileReference reference, string extensionId)>((
                new AnalyzerFileReference(razorSourceGenerator, new SimpleAnalyzerAssemblyLoader()),
                ProjectSystemProject.RazorVsixExtensionId
            ));
        }
    }

    public ImmutableArray<(AnalyzerFileReference reference, string extensionId)> GetAnalyzerReferencesInExtensions()
    {
        return _analyzerReferences;
    }

    private class SimpleAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath)
        {
            // This method is used to add a path that should be probed for analyzer dependencies.
            // In this simple implementation, we do nothing.
        }

        public Assembly LoadFromPath(string fullPath)
        {
            // This method is used to load an analyzer assembly from the specified path.
            // In this simple implementation, we use Assembly.LoadFrom to load the assembly.
            return Assembly.LoadFrom(fullPath);
        }
    }
}
