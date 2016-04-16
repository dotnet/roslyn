// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal static class TestHost
    {
        private static HostServices s_testServices;

        public static HostServices Services
        {
            get
            {
                if (s_testServices == null)
                {
                    var tmp = MefHostServices.Create(MefHostServices.DefaultAssemblies.Concat(new[] { typeof(TestHost).Assembly }));
                    System.Threading.Interlocked.CompareExchange(ref s_testServices, tmp, null);
                }

                return s_testServices;
            }
        }
    }
}
