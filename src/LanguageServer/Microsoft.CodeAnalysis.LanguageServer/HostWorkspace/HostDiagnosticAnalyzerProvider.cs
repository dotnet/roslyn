// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
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
            var razorUtilities = Path.Combine(razorDir, RazorAnalyzerAssemblyResolver.RazorUtilsAssemblyName + ".dll");
            var objectPool = Path.Combine(razorDir, RazorAnalyzerAssemblyResolver.ObjectPoolAssemblyName + ".dll");

            return [
                    (razorSourceGenerator, ProjectSystemProject.RazorVsixExtensionId),
                    (razorUtilities, ProjectSystemProject.RazorVsixExtensionId),
                    (objectPool, ProjectSystemProject.RazorVsixExtensionId)
                 ];
        }
        return [];
    }
}
