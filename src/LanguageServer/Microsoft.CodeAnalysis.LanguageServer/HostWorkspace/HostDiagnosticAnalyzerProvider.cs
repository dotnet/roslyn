// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed class HostDiagnosticAnalyzerProvider(string? razorSourceGenerator) : IHostDiagnosticAnalyzerProvider
{
    public ImmutableArray<(AnalyzerFileReference reference, string extensionId)> GetAnalyzerReferencesInExtensions() => [];

    public ImmutableArray<(string path, string extensionId)> GetRazorAssembliesInExtensions()
    {
        if (File.Exists(razorSourceGenerator))
        {
            // we also have to redirect the utilities and object pool assemblies
            var razorDir = Path.GetDirectoryName(razorSourceGenerator) ?? "";
            var razorUtilities = GetDependency(razorDir, RazorAnalyzerAssemblyResolver.RazorUtilsAssemblyName);
            var objectPool = GetDependency(razorDir, RazorAnalyzerAssemblyResolver.ObjectPoolAssemblyName);

            return
            [
                (razorSourceGenerator, ProjectSystemProject.RazorVsixExtensionId),
                (razorUtilities, ProjectSystemProject.RazorVsixExtensionId),
                (objectPool, ProjectSystemProject.RazorVsixExtensionId)
            ];
        }
        return [];

        static string GetDependency(string razorDir, string dependencyName)
        {
            var dependency = Path.Combine(razorDir, dependencyName + ".dll");
            if (!File.Exists(dependency))
            {
                throw new FileNotFoundException($"Could not find razor dependency {dependency}");
            }
            return dependency;
        }
    }
}
