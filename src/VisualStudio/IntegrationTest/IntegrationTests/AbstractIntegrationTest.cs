// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [CaptureTestName]
    public abstract class AbstractIntegrationTest : IDisposable
    {
        /// <summary>
        /// A long timeout used to avoid hangs in tests, where a test failure manifests as an operation never occurring.
        /// </summary>
        protected static readonly TimeSpan HangMitigatingTimeout = TimeSpan.FromMinutes(1);

        public readonly VisualStudioInstance VisualStudio;

        protected readonly string ProjectName = "TestProj";
        protected readonly string SolutionName = "TestSolution";

        private VisualStudioInstanceContext _visualStudioContext;

        protected AbstractIntegrationTest(
            VisualStudioInstanceFactory instanceFactory)
        {
            Helper.Automation.TransactionTimeout = 20000;
            _visualStudioContext = instanceFactory.GetNewOrUsedInstance(SharedIntegrationHostFixture.RequiredPackageIds);
            VisualStudio = _visualStudioContext.Instance;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Wait(double seconds)
        {
            var timeout = TimeSpan.FromMilliseconds(seconds * 1000);
            Thread.Sleep(timeout);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _visualStudioContext.Dispose();
            }
        }

        protected KeyPress Ctrl(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Ctrl);

        protected KeyPress Shift(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Shift);

        protected KeyPress Alt(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Alt);
    }
}
