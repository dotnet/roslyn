// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.CodeAnalysis
{
    internal sealed class CoreClrAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        private readonly AssemblyLoadContext _loadContext;

        public CoreClrAnalyzerAssemblyLoader()
        {
            _loadContext = AssemblyLoadContext.GetLoadContext(typeof(CoreClrAnalyzerAssemblyLoader).GetTypeInfo().Assembly);

            _loadContext.Resolving += (context, name) =>
            {
                Debug.Assert(ReferenceEquals(context, _loadContext));
                return Load(name.FullName);
            };
        }

        protected override Assembly LoadFromPathImpl(string fullPath) => _loadContext.LoadFromAssemblyPath(fullPath);
    }
}
