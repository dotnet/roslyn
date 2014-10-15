// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    public static class DesktopMefHostServices
    {
        private static MefHostServices defaultServices;
        public static MefHostServices DefaultServices
        {
            get
            {
                if (defaultServices == null)
                {
                    Interlocked.CompareExchange(ref defaultServices, MefHostServices.Create(DefaultAssemblies), null);
                }

                return defaultServices;
            }
        }

        private static ImmutableArray<Assembly> defaultAssemblies;
        private static ImmutableArray<Assembly> DefaultAssemblies
        {
            get
            {
                if (defaultAssemblies == null)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref defaultAssemblies, CreateDefaultAssemblies(), default(ImmutableArray<Assembly>));
                }

                return defaultAssemblies;
            }
        }

        private static ImmutableArray<Assembly> CreateDefaultAssemblies()
        {
            var assemblyNames = new string[]
            {
                "Microsoft.CodeAnalysis.Workspaces.Desktop",
                "Microsoft.CodeAnalysis.CSharp.Workspaces.Desktop",
                "Microsoft.CodeAnalysis.VisualBasic.Workspaces.Desktop",
            };

            return MefHostServices.DefaultAssemblies.Concat(MefHostServices.LoadNearbyAssemblies(assemblyNames)).ToImmutableArray();
        }
    }
}