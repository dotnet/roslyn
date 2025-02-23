// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.OutOfProcess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Remoting;
    using Xunit.Abstractions;
    using Xunit.Harness;
    using Xunit.InProcess;
    using Xunit.Sdk;

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

        public InProcessIdeTestAssemblyRunner CreateTestAssemblyRunner(ITestAssembly testAssembly, IXunitTestCase[] testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            return TestInvokerInProc.CreateTestAssemblyRunner(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions);
        }

        private class TestOutputHelperWrapper : MarshalByRefObject, ITestOutputHelper
        {
            private readonly ITestOutputHelper _testOutputHelper;

            public TestOutputHelperWrapper(ITestOutputHelper testOutputHelper)
            {
                _testOutputHelper = testOutputHelper;
            }

            public void WriteLine(string message)
            {
                _testOutputHelper.WriteLine(message);
            }

            public void WriteLine(string format, params object?[] args)
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
