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
            _inMemoryAssemblyContext = new LoadContext(this, null);
        }

        public override Assembly LoadFromStream(Stream peStream, Stream pdbStream)
        {
            return _inMemoryAssemblyContext.LoadFromStream(peStream, pdbStream);
        }

        public override AssemblyAndLocation LoadFromPath(string path)
        {
            var assembly = new LoadContext(this, Path.GetDirectoryName(path)).LoadFromAssemblyPath(path);
            return new AssemblyAndLocation(assembly, path, fromGac: false);
        }

        public override void Dispose()
        {
            // nop
        }

        private sealed class LoadContext : AssemblyLoadContext
        {
            private readonly string _loadDirectoryOpt;
            private readonly CoreAssemblyLoaderImpl _loader;

            internal LoadContext(CoreAssemblyLoaderImpl loader, string loadDirectoryOpt)
            {
                Debug.Assert(loader != null);
                _loader = loader;
                _loadDirectoryOpt = loadDirectoryOpt;
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                return _loader.Loader.ResolveAssembly(AssemblyIdentity.FromAssemblyReference(assemblyName), _loadDirectoryOpt) ??
                       Default.LoadFromAssemblyName(assemblyName);
            }

            public new Assembly LoadFromStream(Stream assembly, Stream assemblySymbols)
            {
                return base.LoadFromStream(assembly, assemblySymbols);
            }

            public new Assembly LoadFromAssemblyPath(string assemblyPath)
            {
                return base.LoadFromAssemblyPath(assemblyPath);
            }
        }
    }
}
