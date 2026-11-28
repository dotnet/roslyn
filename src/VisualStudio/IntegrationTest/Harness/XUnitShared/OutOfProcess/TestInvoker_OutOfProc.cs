// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.OutOfProcess
{
    using System;
    using Xunit.Harness;
    using Xunit.InProcess;
    using Xunit.v3;

    internal class TestInvoker_OutOfProc : OutOfProcComponent
    {
        internal TestInvoker_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            TestInvokerInProc = CreateInProcComponent<TestInvoker_InProc>(visualStudioInstance);
        }

        internal TestInvoker_InProc TestInvokerInProc
        {
            get;
        }

        public InProcessIdeTestAssemblyRunner CreateTestAssemblyRunner()
        {
            return TestInvokerInProc.CreateTestAssemblyRunner();
        }

        private class TestOutputHelperWrapper : MarshalByRefObject, ITestOutputHelper
        {
            private readonly ITestOutputHelper _testOutputHelper;

            public TestOutputHelperWrapper(ITestOutputHelper testOutputHelper)
            {
                _testOutputHelper = testOutputHelper;
            }

            public string Output => _testOutputHelper.Output;

            public void Write(string message)
            {
                _testOutputHelper.Write(message);
            }

            public void Write(string format, params object[] args)
            {
                _testOutputHelper.Write(format, args);
            }

            public void WriteLine(string message)
            {
                _testOutputHelper.WriteLine(message);
            }

            public void WriteLine(string format, params object[] args)
            {
                _testOutputHelper.WriteLine(format, args);
            }

            // The life of this object is managed explicitly
            public override object? InitializeLifetimeService()
            {
                return null;
            }
        }
    }
}
