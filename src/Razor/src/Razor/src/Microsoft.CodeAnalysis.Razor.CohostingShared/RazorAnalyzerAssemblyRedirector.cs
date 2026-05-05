// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Reflection;
using IRazorAnalyzerAssemblyRedirector = Microsoft.CodeAnalysis.ExternalAccess.Razor.RazorAnalyzerAssemblyRedirector.IRazorAnalyzerAssemblyRedirector;

namespace Microsoft.VisualStudio.Razor;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[Export(typeof(IRazorAnalyzerAssemblyRedirector))]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class RazorAnalyzerAssemblyRedirector : IRazorAnalyzerAssemblyRedirector
{
    private readonly FrozenDictionary<string, string> _compilerAssemblyMap;

#pragma warning disable RS0030 // Do not use banned APIs
    [ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
    public RazorAnalyzerAssemblyRedirector()
    {
        ImmutableArray<(string name, string path)> compilerAssemblyTypes = [
            GetRedirectEntry(typeof(CodeAnalysis.Razor.CompilerFeatures)), // Microsoft.CodeAnalysis.Razor.Compiler
            GetRedirectEntry(typeof(CodeAnalysis.Razor.CompilerFeatures), "Microsoft.NET.Sdk.Razor.SourceGenerators"),
            GetRedirectEntry(typeof(AspNetCore.Razor.ArgHelper)), // Microsoft.AspNetCore.Razor.Utilities.Shared

            // The following dependencies will be provided by the Compiler ALC so its not strictly required to redirect them, but we do so for completeness. 
            GetRedirectEntry(typeof(ImmutableArray)), // System.Collections.Immutable

            // ObjectPool is special
            GetObjectPoolRedirect() // Microsoft.Extensions.ObjectPool
        ];

        _compilerAssemblyMap = compilerAssemblyTypes.ToFrozenDictionary(t => t.name, t => t.path);
    }

    public string? RedirectPath(string fullPath)
    {
        var name = Path.GetFileNameWithoutExtension(fullPath);
        return _compilerAssemblyMap.TryGetValue(name, out var path) ? path : null;
    }

    private static (string name, string path) GetObjectPoolRedirect()
    {
        // Temporary fix for: https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2583134
        // In VS (where this code is running) ObjectPool comes from the shared assemblies folder.

        // When Roslyn wins the race to load Razor it shadow copies the assemblies before resolving them. Roslyn shadow copies assemblies
        // into a directory based on the directory they came from. Because ObjectPool has a different directory path from the rest of
        // our assemblies, we end up with two shadow copy directories: one for ObjectPool and one for everything else. 

        // Roslyn creates an ALC per directory where the assemblies are loaded from.
        // ObjectPool isn't loaded into the same ALC as the rest of the compiler. When we come to use the ObjectPool in the compiler, it
        // has to be resolved because its not already loaded. 
        // This invokes our assembly resolver code (which is currently in the Roslyn EA). However, our resolver expects to find the ObjectPool assembly
        // to load next to the compiler assembly, which it isn't because of the shadow copying. It fails to load ObjectPool. That then means resolution falls back to
        // the ServiceHub loader, which *is* able to successfully load a copy of the assembly from the framework, but into its own ALC. This is the copy of the
        // ObjectPool that the compiler 'binds' against. Call this ObjectPool(1).

        // When Razor tooling starts up, it also wants to load the same assemblies, and goes through the assembly resolver for any razor assemblies.
        // Because it doesn't consider shadow copying, it requests them from a different path than Roslyn loaded them from (the razor language services folder).
        // Because the compiler assemblies are already loaded (from the shadow copy folder) the resolver just returns those assemblies. These are, in fact the
        // same assemblies so this is correct up to this point.
        // However, when the tooling goes to load ObjectPool it calls into the resolver with the path from *the razor language services folder*. The resolver
        // previously failed to load ObjectPool, so it tries again. This time, with the updated path, it is able to find it next to the compiler
        // assembly and successfully load it. Call this ObjectPool(2)

        // That means that the razor tooling has the compiler assemblies and ObjectPool(2), which are all in the same ALC. However, because of the earlier failed
        // load the compiler assemblies are 'bound' to ObjectPool(1). That causes a MissingMethodException when we come to use any of the methods from the compiler
        // assemblies: it is looking for a method bound to ObjectPool(1), but can't find it because it has ObjectPool(2).

        // This doesn't happen if Razor tooling loads first. It is able to resolve ObjectPool at the same time as the compiler assemblies, and 'binds' them. When
        // the source generator is loaded by Roslyn all of the assemblies are already loaded and can just be re-used. 

        // The correct fix is to not shadow copy razor assemblies (see https://github.com/dotnet/razor/issues/12307) which will ensure that the initial resolve
        // is able to find the assembly and load it into the same ALC as the compiler assemblies when Roslyn wins the race.

        // For now, to ensure consistent assembly loading and avoid ALC mismatches, explicitly override the location of ObjectPool to be next to the compiler assemblies.
        // This ensures that when Roslyn does the shadow copy, all assemblies end up in the same directory and ALC, allowing them to be re-used when Razor loads.

        // Get the compiler assembly location
        var (_, compilerAssemblyLocation) = GetRedirectEntry(typeof(CodeAnalysis.Razor.CompilerFeatures));

        // Get the redirect for the object pool, but override its location to be next to the compiler
        var objectPoolRedirect = GetRedirectEntry(typeof(Microsoft.Extensions.ObjectPool.ObjectPool));
        objectPoolRedirect.path = Path.Combine(Path.GetDirectoryName(compilerAssemblyLocation)!, $"{objectPoolRedirect.name}.dll");
        return objectPoolRedirect;
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
