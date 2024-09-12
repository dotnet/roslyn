// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.MSBuild.Build;

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
                // All of our MEF types are actually in Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost
                typeof(ProjectBuildManager).Assembly.GetName().Name,
            };

            return MefHostServices.DefaultAssemblies.Concat(
                MefHostServicesHelpers.LoadNearbyAssemblies(assemblyNames));
        }

        internal readonly struct TestAccessor
        {
            /// <summary>
            /// Allows tests to clear services between runs.
            /// </summary>
            internal static void ClearCachedServices()
            {
                // The existing host, if any, is not retained past this call.
                s_defaultServices = null;
            }
        }
    }
}
