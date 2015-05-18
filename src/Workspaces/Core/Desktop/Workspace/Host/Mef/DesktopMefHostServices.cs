// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    public static class DesktopMefHostServices
    {
        private static MefHostServices s_defaultServices;
        public static MefHostServices DefaultServices
        {
            get
            {
                if (s_defaultServices == null)
                {
                    Interlocked.CompareExchange(ref s_defaultServices, MefHostServices.Create(DefaultAssemblies), null);
                }

                return s_defaultServices;
            }
        }

        private static ImmutableArray<Assembly> s_defaultAssemblies;
        private static ImmutableArray<Assembly> DefaultAssemblies
        {
            get
            {
                if (s_defaultAssemblies == null)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref s_defaultAssemblies, CreateDefaultAssemblies(), default(ImmutableArray<Assembly>));
                }

                return s_defaultAssemblies;
            }
        }

        private static ImmutableArray<Assembly> CreateDefaultAssemblies()
        {
            var assemblyNames = new string[]
            {
                "Microsoft.CodeAnalysis.Workspaces.Desktop",
            };

            return MefHostServices.DefaultAssemblies.Concat(MefHostServices.LoadNearbyAssemblies(assemblyNames));
        }
    }
}
