// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [CaptureTestName]
    public abstract class AbstractIntegrationTest : IDisposable
    {
        public readonly VisualStudioInstanceContext VisualStudio;
        protected readonly VisualStudioWorkspace_OutOfProc VisualStudioWorkspaceOutOfProc;
        protected readonly TextViewWindow_OutOfProc TextViewWindow;

        protected AbstractIntegrationTest(
            VisualStudioInstanceFactory instanceFactory,
            Func<VisualStudioInstanceContext, TextViewWindow_OutOfProc> textViewWindowBuilder)
        {
            VisualStudio = instanceFactory.GetNewOrUsedInstance(SharedIntegrationHostFixture.RequiredPackageIds);
            TextViewWindow = textViewWindowBuilder(VisualStudio);
            VisualStudioWorkspaceOutOfProc = VisualStudio.Instance.VisualStudioWorkspace;
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
    }
}
