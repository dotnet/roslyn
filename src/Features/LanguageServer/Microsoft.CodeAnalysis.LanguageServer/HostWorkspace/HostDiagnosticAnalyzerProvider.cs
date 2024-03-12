// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
internal class HostDiagnosticAnalyzerProvider : IHostDiagnosticAnalyzerProvider
{
    /// <summary>
    /// The full path to the Razor source generator
    /// </summary>
    public string? RazorSourceGenerator { get; set; }

    private ImmutableArray<(AnalyzerFileReference reference, string extensionId)>? _cachedAnalyzerReferences;

    public HostDiagnosticAnalyzerProvider(string? razorSourceGenerator)
    {
        RazorSourceGenerator = razorSourceGenerator;
    }

    public ImmutableArray<(AnalyzerFileReference reference, string extensionId)> GetAnalyzerReferencesInExtensions()
    {
        if (_cachedAnalyzerReferences != null)
        {
            return _cachedAnalyzerReferences.Value;
        }

        if (RazorSourceGenerator == null)
        {
            _cachedAnalyzerReferences = ImmutableArray<(AnalyzerFileReference reference, string extensionId)>.Empty;
            return _cachedAnalyzerReferences.Value;
        }

        var analyzerReferences = new List<(AnalyzerFileReference, string)>();

        // Create an AnalyzerFileReference for each file
        var analyzerReference = new AnalyzerFileReference(RazorSourceGenerator, new SimpleAnalyzerAssemblyLoader());

        // Add the reference to the list, using the file name as the extension ID
        analyzerReferences.Add((analyzerReference, ProjectSystemProject.RazorVsixExtensionId));

        _cachedAnalyzerReferences = analyzerReferences.ToImmutableArray();
        return _cachedAnalyzerReferences.Value;
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
