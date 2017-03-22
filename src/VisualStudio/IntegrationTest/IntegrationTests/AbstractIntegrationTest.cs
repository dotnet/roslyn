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
        protected readonly VisualStudioInstanceContext VisualStudio;

        protected AbstractIntegrationTest(VisualStudioInstanceFactory instanceFactory)
        {
            VisualStudio = instanceFactory.GetNewOrUsedInstance(SharedIntegrationHostFixture.RequiredPackageIds);
        }

        public void Dispose()
            => VisualStudio.Dispose();

        protected void Wait(double seconds)
        {
            var timeout = TimeSpan.FromMilliseconds(seconds * 1000);
            Thread.Sleep(timeout);
        }

        protected KeyPress KeyPress(VirtualKey virtualKey, ShiftState shiftState)
            => new KeyPress(virtualKey, shiftState);

        protected KeyPress Ctrl(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Ctrl);

        protected KeyPress Shift(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Shift);

        protected KeyPress Alt(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Alt);

        protected void ExecuteCommand(string commandName, string argument = "")
            => VisualStudio.Instance.ExecuteCommand(commandName, argument);
    }
}
