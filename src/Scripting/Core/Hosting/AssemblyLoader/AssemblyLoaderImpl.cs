// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal abstract class AssemblyLoaderImpl(InteractiveAssemblyLoader loader) : IDisposable
    {
        internal readonly InteractiveAssemblyLoader Loader = loader;

        public static AssemblyLoaderImpl Create(InteractiveAssemblyLoader loader)
#if NET
            => new CoreAssemblyLoaderImpl(loader);
#else
            => new DesktopAssemblyLoaderImpl(loader);
#endif 

        public abstract Assembly LoadFromStream(Stream peStream, Stream pdbStream);
        public abstract AssemblyAndLocation LoadFromPath(string path);
        public abstract void Dispose();
    }
}
