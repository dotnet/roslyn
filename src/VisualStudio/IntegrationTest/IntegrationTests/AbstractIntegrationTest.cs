// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Roslyn.VisualStudio.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public abstract class AbstractIntegrationTest : IDisposable
    {
        protected readonly VisualStudioInstanceContext VisualStudio;

        protected AbstractIntegrationTest(VisualStudioInstanceFactory instanceFactory)
        {
            VisualStudio = instanceFactory.GetNewOrUsedInstance(SharedIntegrationHostFixture.RequiredPackageIds);
        }

        public void Dispose()
        {
            VisualStudio.Dispose();
        }

        protected void Wait(double seconds)
        {
            var timeout = TimeSpan.FromMilliseconds(seconds * 1000);
            Thread.Sleep(timeout);
        }
    }
}
