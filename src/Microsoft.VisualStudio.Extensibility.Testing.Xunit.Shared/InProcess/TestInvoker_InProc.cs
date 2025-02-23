// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.InProcess
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Xunit.Abstractions;
    using Xunit.Harness;
    using Xunit.Sdk;
    using Xunit.Threading;

    internal class TestInvoker_InProc : InProcComponent
    {
        private TestInvoker_InProc()
        {
            AppDomain.CurrentDomain.AssemblyResolve += VisualStudioInstanceFactory.AssemblyResolveHandler;
        }

        // NOTE: This is called by OutOfProcComponent.CreateInProcComponent using Activator.
        public static TestInvoker_InProc Create()
            => new TestInvoker_InProc();

        public InProcessIdeTestAssemblyRunner CreateTestAssemblyRunner(ITestAssembly testAssembly, IXunitTestCase[] testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            return new InProcessIdeTestAssemblyRunner(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions);
        }
    }
}
