// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.Format
{
    internal static class FormatMefHostServices
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
                typeof(FormatMefHostServices).Assembly.GetName().Name,
            };

            return MefHostServices.DefaultAssemblies.Concat(
                MefHostServices.LoadNearbyAssemblies(assemblyNames));
        }
    }
}
