// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using System;
using System.Threading;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [CaptureTestName]
    public abstract class AbstractIntegrationTest : IDisposable
    {
        public readonly VisualStudioInstanceContext VisualStudio;
        public readonly VisualStudioWorkspace_OutOfProc VisualStudioWorkspaceOutOfProc;
        public readonly TextViewWindow_OutOfProc TextViewWindow;

        protected readonly string ProjectName = "TestProj";
        protected readonly string SolutionName = "TestSolution";

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

        protected KeyPress Ctrl(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Ctrl);

        protected KeyPress Shift(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Shift);

        protected KeyPress Alt(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Alt);
    }
}