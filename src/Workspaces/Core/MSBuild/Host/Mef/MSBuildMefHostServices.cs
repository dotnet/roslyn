﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using System.Threading;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    public static class MSBuildMefHostServices
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
        public static ImmutableArray<Assembly> DefaultAssemblies
        {
            get
            {
                if (s_defaultAssemblies == null)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref s_defaultAssemblies, CreateDefaultAssemblies(), default);
                }

                return s_defaultAssemblies;
            }
        }

        private static ImmutableArray<Assembly> CreateDefaultAssemblies()
        {
            var assemblyNames = new string[]
            {
                typeof(MSBuildMefHostServices).Assembly.GetName().Name,
            };

            return MefHostServices.DefaultAssemblies.Concat(
                MefHostServices.LoadNearbyAssemblies(assemblyNames));
        }
    }
}
