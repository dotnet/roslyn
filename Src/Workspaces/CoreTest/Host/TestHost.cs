// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UnitTests.Host.WorkspaceServices.Caching;
using Microsoft.CodeAnalysis.UnitTests.Persistence;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.UnitTests
{
    internal static class TestHost
    {
        private static HostServices testServices;

        public static HostServices Services
        {
            get
            {
                if (testServices == null)
                {
                    var tmp = MefHostServices.Create(MefHostServices.DefaultAssemblies.Concat(new[] { typeof(TestHost).Assembly }));
                    System.Threading.Interlocked.CompareExchange(ref testServices, tmp, null);
                }

                return testServices;
            }
        }
    }
}
