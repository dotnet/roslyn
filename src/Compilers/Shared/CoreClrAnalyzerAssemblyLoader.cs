// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if NETCOREAPP2_1

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.CodeAnalysis
{
    internal class CoreClrAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        private AssemblyLoadContext _loadContext;

        public CoreClrAnalyzerAssemblyLoader()
        {
        }

        protected override Assembly LoadFromPathImpl(string fullPath)
        {
            //.NET Native doesn't support AssemblyLoadContext.GetLoadContext. 
            // Initializing the _loadContext in the .ctor would cause
            // .NET Native builds to fail because the .ctor is called. 
            // However, LoadFromPathImpl is never called in .NET Native, so 
            // we do a lazy initialization here to make .NET Native builds happy.
            if (_loadContext == null)
            {
                AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(typeof(CoreClrAnalyzerAssemblyLoader).GetTypeInfo().Assembly);

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
