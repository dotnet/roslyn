// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal sealed class CoreAssemblyLoaderImpl : AssemblyLoaderImpl
    {
        private readonly LoadContext _inMemoryAssemblyContext;

        internal CoreAssemblyLoaderImpl(InteractiveAssemblyLoader loader)
            : base(loader)
        {
            _inMemoryAssemblyContext = new LoadContext(Loader, null);
        }

        public override Assembly LoadFromStream(Stream peStream, Stream pdbStream)
        {
            return _inMemoryAssemblyContext.LoadFromStream(peStream, pdbStream);
        }

        public override AssemblyAndLocation LoadFromPath(string path)
        {
            // Create a new context that knows the directory where the assembly was loaded from
            // and uses it to resolve dependencies of the assembly. We could create one context per directory,
            // but there is no need to reuse contexts.
            var assembly = new LoadContext(Loader, Path.GetDirectoryName(path)).LoadFromAssemblyPath(path);

            return new AssemblyAndLocation(assembly, path, fromGac: false);
        }

        public override void Dispose()
        {
            // nop
        }

        private sealed class LoadContext : AssemblyLoadContext
        {
            private readonly string _loadDirectoryOpt;
            private readonly InteractiveAssemblyLoader _loader;

            internal LoadContext(InteractiveAssemblyLoader loader, string loadDirectoryOpt)
            {
                Debug.Assert(loader != null);

                _loader = loader;
                _loadDirectoryOpt = loadDirectoryOpt;

                // CoreCLR resolves assemblies in steps:
                //
                //   1) Call AssemblyLoadContext.Load -- our context returns null
                //   2) TPA list
                //   3) Default.Resolving event
                //   4) AssemblyLoadContext.Resolving event -- hooked below
                // 
                // What we want is to let the default context load assemblies it knows about (this includes already loaded assemblies,
                // assemblies in AppPath, platform assemblies, assemblies explciitly resolved by the App by hooking Default.Resolving, etc.).
                // Only if the assembly can't be resolved that way, the interactive resolver steps in.
                //
                // This order is necessary to avoid loading assemblies twice (by the host App and by interactive loader).

                Resolving += (_, assemblyName) =>
                    _loader.ResolveAssembly(AssemblyIdentity.FromAssemblyReference(assemblyName), _loadDirectoryOpt);
            }

            protected override Assembly Load(AssemblyName assemblyName) => null;
        }
    }
}
