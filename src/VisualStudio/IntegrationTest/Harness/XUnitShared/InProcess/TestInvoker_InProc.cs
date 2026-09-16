// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.InProcess
{
    using System;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Xunit.Harness;

    internal class TestInvoker_InProc : InProcComponent
    {
        private TestInvoker_InProc()
        {
            AppDomain.CurrentDomain.AssemblyResolve += VisualStudioInstanceFactory.AssemblyResolveHandler;
        }

        // NOTE: This is called by OutOfProcComponent.CreateInProcComponent using Activator.
        public static TestInvoker_InProc Create()
            => new TestInvoker_InProc();

        public TestExecutionResult RunTests(TestExecutionRequest request, TestExecutionMessageSink messageSink)
        {
            var dispatcher = Application.Current?.Dispatcher
                ?? throw new InvalidOperationException("The Visual Studio WPF dispatcher is unavailable.");

#pragma warning disable VSTHRD001 // The remote component's synchronous API requires waiting for the dispatcher operation.
            var result = dispatcher
                .InvokeAsync(
                    () => InProcessIdeTestAssemblyRunner.RunAsync(request, messageSink),
                    DispatcherPriority.Background)
                .Task
                .Unwrap()
                .GetAwaiter()
                .GetResult();
#pragma warning restore VSTHRD001 // Await JoinableTaskFactory.SwitchToMainThreadAsync
            return result;
        }
    }
}
