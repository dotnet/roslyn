// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.CodeAnalysis
{
    internal class DefaultAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        private AssemblyLoadContext _loadContext;

        protected override Assembly LoadFromPathImpl(string fullPath)
        {
            //.NET Native doesn't support AssemblyLoadContext.GetLoadContext. 
            // Initializing the _loadContext in the .ctor would cause
            // .NET Native builds to fail because the .ctor is called. 
            // However, LoadFromPathImpl is never called in .NET Native, so 
            // we do a lazy initialization here to make .NET Native builds happy.
            if (_loadContext == null)
            {
                AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(typeof(DefaultAnalyzerAssemblyLoader).GetTypeInfo().Assembly);

                if (System.Threading.Interlocked.CompareExchange(ref _loadContext, loadContext, null) == null)
                {
                    _loadContext.Resolving += (context, name) =>
                    {
                        Debug.Assert(ReferenceEquals(context, _loadContext));
                        return Load(name.FullName);
                    };
                }
            }

            return LoadImpl(fullPath);
        }

        protected virtual Assembly LoadImpl(string fullPath) => _loadContext.LoadFromAssemblyPath(fullPath);
    }
}

#endif
