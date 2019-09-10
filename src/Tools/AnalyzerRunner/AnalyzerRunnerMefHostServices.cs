// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;

namespace AnalyzerRunner
{
    internal static class AnalyzerRunnerMefHostServices
    {
        private static MefHostServices s_defaultServices;

        public static MefHostServices DefaultServices
        {
            get
            {
                if (s_defaultServices is null)
                {
                    Interlocked.CompareExchange(ref s_defaultServices, MefHostServices.Create(DefaultAssemblies), null);
                }

                return s_defaultServices;
            }
        }

        private static ImmutableArray<Assembly> DefaultAssemblies
            => MSBuildMefHostServices.DefaultAssemblies.Add(typeof(AnalyzerRunnerMefHostServices).Assembly);
    }
}
