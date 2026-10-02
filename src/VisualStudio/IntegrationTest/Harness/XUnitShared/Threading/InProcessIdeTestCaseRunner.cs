// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Xunit.Harness;
    using Xunit.InProcess;
    using Xunit.v3;

    public class InProcessIdeTestCaseRunner : XunitTestCaseRunner
    {
        public static new readonly InProcessIdeTestCaseRunner Instance = new InProcessIdeTestCaseRunner();

        protected override async ValueTask<RunSummary> RunTest(XunitTestCaseRunnerContext ctxt, IXunitTest test)
        {
            DataCollectionService.InstallFirstChanceExceptionHandler();
            VisualStudio_InProc.Create().ActivateMainWindow();

            var synchronizationContext = new DispatcherSynchronizationContext(Application.Current.Dispatcher, DispatcherPriority.Background);
            var taskScheduler = new SynchronizationContextTaskScheduler(synchronizationContext);
            try
            {
#pragma warning disable CA1062 // Validate arguments of public methods
                DataCollectionService.CurrentTest = test;
#pragma warning restore CA1062 // Validate arguments of public methods
                return await Task.Factory.StartNew(
                    async () => await base.RunTest(ctxt, test).ConfigureAwait(true),
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    taskScheduler).Unwrap().ConfigureAwait(true);
            }
            finally
            {
                DataCollectionService.CurrentTest = null;
            }
        }
    }
}
