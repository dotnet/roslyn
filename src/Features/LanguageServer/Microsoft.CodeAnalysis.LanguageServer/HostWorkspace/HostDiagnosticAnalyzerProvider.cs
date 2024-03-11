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
    /// The directory where the analyzers are located
    /// </summary>
    public string? AnalyzerDirectory { get; set; }

    private const string RazorVsixExtensionId = "Microsoft.VisualStudio.RazorExtension";
    private static readonly HashSet<string> s_razorSourceGeneratorAssemblyNames = new[] {
        "Microsoft.NET.Sdk.Razor.SourceGenerators",
        "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators",
        "Microsoft.CodeAnalysis.Razor.Compiler",
    }.ToHashSet<string>();

    public HostDiagnosticAnalyzerProvider(string? analyzerDirectory)
    {
        AnalyzerDirectory = analyzerDirectory;
    }

    public ImmutableArray<(AnalyzerFileReference reference, string extensionId)> GetAnalyzerReferencesInExtensions()
    {
        if (AnalyzerDirectory == null)
        {
            return ImmutableArray<(AnalyzerFileReference reference, string extensionId)>.Empty;
        }

        var analyzerReferences = new List<(AnalyzerFileReference, string)>();

        // Get all the .dll files in the directory
        var analyzerFiles = Directory.GetFiles(AnalyzerDirectory, "*.dll");

        foreach (var analyzerFile in analyzerFiles)
        {
            // Create an AnalyzerFileReference for each file
            var analyzerReference = new AnalyzerFileReference(analyzerFile, new SimpleAnalyzerAssemblyLoader());

            if (s_razorSourceGeneratorAssemblyNames.Any(
                name => analyzerReference.FullPath.Contains(name, StringComparison.OrdinalIgnoreCase)))
            {
                // Add the reference to the list, using the file name as the extension ID
                analyzerReferences.Add((analyzerReference, RazorVsixExtensionId));
            }
        }

        return analyzerReferences.ToImmutableArray();
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
