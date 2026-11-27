// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RazorAssemblyLoadContext : AssemblyLoadContext
{
    internal const string RemoteServiceHubAssemblyName = "Microsoft.CodeAnalysis.Remote.ServiceHub";
    internal const string RemoteWorkspacesAssemblyName = "Microsoft.CodeAnalysis.Remote.Workspaces";
    internal const string MessagePackAssemblyName = "MessagePack";
    internal const string NerdbankStreamsAssemblyName = "Nerdbank.Streams";
    internal const string NewtonsoftJsonAssemblyName = "Newtonsoft.Json";
    internal const string StreamJsonRpcAssemblyName = "StreamJsonRpc";
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

    private RazorAssemblyLoadContext(AssemblyLoadContext parent, string baseDirectory, bool isCollectible)
        : base(isCollectible)
    {
        _parent = parent;
        _baseDirectory = baseDirectory;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // `Remote.Razor` needs its own ALC so it can intercept the Razor-owned closure under `_baseDirectory`,
        // but the assemblies below must stay shared with the parent ALC. `Remote.ServiceHub` is the parent-host
        // entry point for this flow, `RazorAnalyzerAssemblyResolver` intentionally hands the Razor compiler/tooling
        // assemblies to Roslyn's compiler load context, and `RazorServices` builds brokered-service descriptors via
        // `Remote.Workspaces`' `ServiceDescriptors` and `RemoteSerializationOptions`. Those descriptors close over the
        // MessagePack/Nerdbank.Streams/Newtonsoft.Json/StreamJsonRpc stack. If any part of that graph is loaded again
        // into the Razor ALC, CLR type identity splits across contexts and brokered service initialization fails with
        // `MissingMethodException`/`TypeInitializationException`.
        if (assemblyName.Name is RemoteServiceHubAssemblyName
            or RemoteWorkspacesAssemblyName
            or MessagePackAssemblyName
            or NerdbankStreamsAssemblyName
            or NewtonsoftJsonAssemblyName
            or StreamJsonRpcAssemblyName)
        {
            // These are shared host infrastructure and serialization assemblies. Reuse the parent copy
            // rather than loading a second copy into the Razor ALC.
            return _parent.LoadFromAssemblyName(assemblyName);
        }

        // If the assembly is in our root directory, then it's one of ours.
        var fileName = Path.Combine(_baseDirectory, assemblyName.Name + ".dll");
        if (File.Exists(fileName))
        {
            // Some assemblies in the published Razor closure actually belong to Roslyn's compiler load context.
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

    internal static class TestAccessor
    {
        internal static RazorAssemblyLoadContext Create(AssemblyLoadContext parent, string baseDirectory, bool isCollectible = true)
            => new(parent, baseDirectory, isCollectible);
    }
}
#endif
