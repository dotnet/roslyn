// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Roslyn.Test.Utilities.CoreClr
{
    public static class AssemblyLoadContextUtils
    {
        public static SimpleAssemblyLoadContext Create(string name, string? probingPath = null) =>
            new SimpleAssemblyLoadContext(name, probingPath);
    }

    public sealed class SimpleAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _probingPath;

        public SimpleAssemblyLoadContext(string name, string? probingPath = null)
            : base(name, isCollectible: false)
        {
            _probingPath = probingPath ?? Path.GetDirectoryName(typeof(SimpleAssemblyLoadContext).Assembly.Location)!;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = Path.Combine(_probingPath, $"{assemblyName.Name}.dll");
            if (File.Exists(assemblyPath))
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }
    }
}

#endif
