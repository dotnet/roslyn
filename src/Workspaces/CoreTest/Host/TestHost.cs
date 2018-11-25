// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Host
{
    internal static class TestHost
    {
        private static readonly ImmutableArray<Assembly> s_assemblies = MefHostServices.DefaultAssemblies.Add(typeof(TestHost).Assembly);
        private static ComposableCatalog s_catalog;

        public static ImmutableArray<Assembly> Assemblies
            => s_assemblies;

        public static ComposableCatalog Catalog
        {
            get
            {
                if (s_catalog == null)
                {
                    var tmp = ExportProviderCache.GetOrCreateAssemblyCatalog(Assemblies);
                    System.Threading.Interlocked.CompareExchange(ref s_catalog, tmp, null);
                }

                return s_catalog;
            }
        }
    }
}
