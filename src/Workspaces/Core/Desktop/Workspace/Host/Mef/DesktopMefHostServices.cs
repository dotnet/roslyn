// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;

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

        internal static void ResetHostServicesTestOnly()
        {
            s_defaultServices = null;
        }

        public static ImmutableArray<Assembly> DefaultAssemblies => MefHostServices.DefaultAssemblies;
    }
}
