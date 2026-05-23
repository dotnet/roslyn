// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RazorAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyLoadContext _parent;
    private readonly string _baseDirectory;

    public static readonly RazorAssemblyLoadContext Instance = new();

    public RazorAssemblyLoadContext()
        : base(isCollectible: false)
    {
        var thisAssembly = GetType().Assembly!;
        _parent = GetLoadContext(thisAssembly) ?? throw new InvalidOperationException("Unexpected");
        _baseDirectory = Path.GetDirectoryName(thisAssembly.Location) ?? throw new InvalidOperationException("Could not determine base directory");
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // If the assembly is in our root directory, then it's one of ours.
        var fileName = Path.Combine(_baseDirectory, assemblyName.Name + ".dll");
        if (File.Exists(fileName))
        {
            // Is the Roslyn side is responsible for it?
            if (RazorAnalyzerAssemblyResolver.ResolveRazorAssembly(assemblyName, _baseDirectory) is Assembly resolvedAssembly)
            {
                return resolvedAssembly;
            }

            // We're responsible for this one. We load it into our ALC rather than the parent so that we
            // can still intercept the loads of any dependencies which Roslyn might be responsible for.
            return LoadFromAssemblyPath(fileName);
        }

        // This isn't one of our own assemblies, just defer back to the parent ALC.
        return _parent.LoadFromAssemblyName(assemblyName);
    }
}
#endif
