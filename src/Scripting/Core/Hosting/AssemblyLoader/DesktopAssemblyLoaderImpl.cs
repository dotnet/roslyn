// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NET

using System;
using System.IO;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal sealed class DesktopAssemblyLoaderImpl : AssemblyLoaderImpl
    {
        public DesktopAssemblyLoaderImpl(InteractiveAssemblyLoader loader)
            : base(loader)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public override void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
            => Loader.ResolveAssembly(args.Name, args.RequestingAssembly);

        public override Assembly LoadFromStream(Stream peStream, Stream pdbStream)
        {
            var peImage = new byte[peStream.Length];
            peStream.TryReadAll(peImage, 0, peImage.Length);

            if (pdbStream != null)
            {
                var pdbImage = new byte[pdbStream.Length];
                pdbStream.TryReadAll(pdbImage, 0, pdbImage.Length);

                return Assembly.Load(peImage, pdbImage);
            }

            return Assembly.Load(peImage);
        }

        public override AssemblyAndLocation LoadFromPath(string path)
        {
            // An assembly is loaded into CLR's Load Context if it is in the GAC, otherwise it's loaded into No Context via Assembly.LoadFile(string).
            // Assembly.LoadFile(string) automatically redirects to GAC if the assembly has a strong name and there is an equivalent assembly in GAC. 

            var assembly = Assembly.LoadFile(path);
            return new AssemblyAndLocation(assembly, assembly.Location, assembly.GlobalAssemblyCache);
        }
    }
}
#endif
