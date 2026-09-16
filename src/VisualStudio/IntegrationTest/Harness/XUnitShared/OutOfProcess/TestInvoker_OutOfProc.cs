// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.OutOfProcess
{
    using Xunit.Harness;
    using Xunit.InProcess;

    internal class TestInvoker_OutOfProc : OutOfProcComponent
    {
        internal TestInvoker_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            TestInvokerInProc = CreateInProcComponent<TestInvoker_InProc>(visualStudioInstance);
        }

        internal TestInvoker_InProc TestInvokerInProc { get; }

        public TestExecutionResult RunTests(TestExecutionRequest request, TestExecutionMessageSink messageSink)
            => TestInvokerInProc.RunTests(request, messageSink);
    }
}
