// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Workspaces.AnalyzerRedirecting;

namespace Microsoft.VisualStudio.Razor;

#pragma warning disable RS0030 // Do not use banned APIs
[Export(typeof(IAnalyzerAssemblyRedirector)), Shared]
[method: ImportingConstructor]
internal sealed class RazorAnalyzerAssemblyRedirector([Import(AllowDefault = true)] Lazy<RazorAnalyzerAssemblyRedirector.IRazorAnalyzerAssemblyRedirector>? razorRedirector) : IAnalyzerAssemblyRedirector
#pragma warning restore RS0030 // Do not use banned APIs
{
    public string? RedirectPath(string fullPath)
    {
        if (fullPath.IndexOf("razor", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return razorRedirector?.Value.RedirectPath(fullPath);
        }

        return null;
    }

    internal interface IRazorAnalyzerAssemblyRedirector
    {
        string? RedirectPath(string fullPath);
    }
}

// The implementation references Razor compiler types, so keep it behind a lazy import.
#pragma warning disable RS0030 // Do not use banned APIs
[Export(typeof(RazorAnalyzerAssemblyRedirector.IRazorAnalyzerAssemblyRedirector)), Shared]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class RazorCompilerAnalyzerAssemblyRedirector : RazorAnalyzerAssemblyRedirector.IRazorAnalyzerAssemblyRedirector
{
    private readonly FrozenDictionary<string, string> _compilerAssemblyMap = CreateCompilerAssemblyMap();

    public string? RedirectPath(string fullPath)
    {
        var name = Path.GetFileNameWithoutExtension(fullPath);
        return _compilerAssemblyMap.TryGetValue(name, out var path) ? path : null;
    }

    private static FrozenDictionary<string, string> CreateCompilerAssemblyMap()
    {
        ImmutableArray<(string name, string path)> compilerAssemblyTypes = [
            GetRedirectEntry(typeof(CodeAnalysis.Razor.CompilerFeatures)), // Microsoft.CodeAnalysis.Razor.Compiler
            GetRedirectEntry(typeof(CodeAnalysis.Razor.CompilerFeatures), "Microsoft.NET.Sdk.Razor.SourceGenerators"),
            GetRedirectEntry(typeof(AspNetCore.Razor.Optional<int>)), // Microsoft.AspNetCore.Razor.Utilities.Shared

            // The following dependencies will be provided by the Compiler ALC so its not strictly required to redirect them, but we do so for completeness.
            GetRedirectEntry(typeof(ImmutableArray)), // System.Collections.Immutable
        ];

        return compilerAssemblyTypes.ToFrozenDictionary(t => t.name, t => t.path);
    }

    private static (string name, string path) GetRedirectEntry(Type type, string? overrideName = null)
    {
        return (
            name: overrideName ?? type.Assembly.GetName().Name!,
            path: GetAssemblyLocation(type.Assembly)
            );
    }

    private static string GetAssemblyLocation(Assembly assembly)
    {
        var location = assembly.Location;
        var name = Path.GetFileName(location);
        var directory = Path.GetDirectoryName(location) ?? "";

        // In VS on windows, depending on who wins the race to load these assemblies, the base directory will either be the tooling root (if Roslyn wins)
        // or the ServiceHubCore subfolder (razor). In the root directory these are netstandard2.0 targeted, in ServiceHubCore they are .NET targeted.
        // We need to always pick the same set of assemblies regardless of who causes us to load. Because this code only runs in a .NET based host,
        // we want to prefer the .NET targeted ServiceHubCore versions if they exist.
        var serviceHubCoreVersion = Path.Combine(directory, "ServiceHubCore", name);
        if (File.Exists(serviceHubCoreVersion))
        {
            return serviceHubCoreVersion;
        }

        return location;
    }
}
