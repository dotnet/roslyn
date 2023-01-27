// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Microsoft.VisualStudio.Extensibility.Testing.Xunit.IntegrationTests
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using global::Xunit;
    using global::Xunit.Threading;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;
    using _DTE = EnvDTE._DTE;
    using DTE = EnvDTE.DTE;

    public class IdeFactTest : AbstractIdeIntegrationTest
    {
        [IdeFact]
        public async Task TestOpenAndCloseIDE()
        {
            Assert.Equal("devenv", Process.GetCurrentProcess().ProcessName);
            var dte = await TestServices.Shell.GetRequiredGlobalServiceAsync<_DTE, DTE>(HangMitigatingCancellationToken);
            Assert.NotNull(dte);
        }

        [IdeFact]
        public void TestRunsOnUIThread()
        {
            Assert.True(Application.Current.Dispatcher.CheckAccess());
        }

        [IdeFact]
        public async Task TestRunsOnUIThreadAsync()
        {
            Assert.True(Application.Current.Dispatcher.CheckAccess());
            await Task.Yield();
            Assert.True(Application.Current.Dispatcher.CheckAccess());
        }

        [IdeFact]
        public async Task TestYieldsToWorkAsync()
        {
            Assert.True(Application.Current.Dispatcher.CheckAccess());
            await Task.Factory.StartNew(
                () => { },
                CancellationToken.None,
                TaskCreationOptions.None,
                new SynchronizationContextTaskScheduler(new DispatcherSynchronizationContext(Application.Current.Dispatcher)));
            Assert.True(Application.Current.Dispatcher.CheckAccess());
        }

        [IdeFact]
        public async Task TestJoinableTaskFactoryAsync()
        {
            Assert.NotNull(JoinableTaskContext);
            Assert.NotNull(JoinableTaskFactory);
            Assert.Equal(Thread.CurrentThread, JoinableTaskContext.MainThread);

            await TaskScheduler.Default;

            Assert.NotEqual(Thread.CurrentThread, JoinableTaskContext.MainThread);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            Assert.Equal(Thread.CurrentThread, JoinableTaskContext.MainThread);
        }

        [IdeFact(MaxVersion = VisualStudioVersion.VS2012)]
        public async Task TestJoinableTaskFactoryProvidedByTest()
        {
            var taskSchedulerServiceObject = await TestServices.Shell.GetRequiredGlobalServiceAsync<SVsTaskSchedulerService, IVsTaskSchedulerService>(HangMitigatingCancellationToken);
            Assert.NotNull(taskSchedulerServiceObject);
            Assert.Null(taskSchedulerServiceObject as IVsTaskSchedulerService2);

            Assert.NotNull(JoinableTaskContext);
        }

        [IdeFact(MinVersion = VisualStudioVersion.VS2013)]
        public async Task TestJoinableTaskFactoryObtainedFromEnvironment()
        {
            var taskSchedulerServiceObject = await TestServices.Shell.GetRequiredGlobalServiceAsync<SVsTaskSchedulerService, IVsTaskSchedulerService>(HangMitigatingCancellationToken);
            Assert.NotNull(taskSchedulerServiceObject);
            var taskSchedulerService = taskSchedulerServiceObject as IVsTaskSchedulerService2;
            Assert.NotNull(taskSchedulerService);
            Assert.Same(JoinableTaskContext, taskSchedulerService!.GetAsyncTaskContext());
        }

        /// <summary>
        /// ⚠️ Running this test locally will reset the setting for the non-experimental instance.
        /// </summary>
        [IdeFact(RootSuffix = "")]
        public async Task TestStandardInstance()
        {
            var appCommandLine = await TestServices.Shell.GetRequiredGlobalServiceAsync<SVsAppCommandLine, IVsAppCommandLine>(HangMitigatingCancellationToken);

            Assert.Equal(0, appCommandLine.GetOption("rootSuffix", out var present, out var value));
            Assert.Equal(0, present);
            Assert.Null(value);

            Assert.Null(Environment.GetEnvironmentVariable("VSROOTSUFFIX"));
        }

        [IdeFact]
        public async Task TestDefaultExperimentalInstance1()
        {
            var appCommandLine = await TestServices.Shell.GetRequiredGlobalServiceAsync<SVsAppCommandLine, IVsAppCommandLine>(HangMitigatingCancellationToken);

            Assert.Equal(0, appCommandLine.GetOption("rootSuffix", out var present, out var value));
            Assert.Equal(1, present);
            Assert.Equal("Exp", value);

            Assert.Equal("Exp", Environment.GetEnvironmentVariable("VSROOTSUFFIX"));
        }

        [IdeFact(RootSuffix = null)]
        public async Task TestDefaultExperimentalInstance2()
        {
            var appCommandLine = await TestServices.Shell.GetRequiredGlobalServiceAsync<SVsAppCommandLine, IVsAppCommandLine>(HangMitigatingCancellationToken);

            Assert.Equal(0, appCommandLine.GetOption("rootSuffix", out var present, out var value));
            Assert.Equal(1, present);
            Assert.Equal("Exp", value);

            Assert.Equal("Exp", Environment.GetEnvironmentVariable("VSROOTSUFFIX"));
        }

        [IdeFact(RootSuffix = "Exp")]
        public async Task TestExperimentalInstance()
        {
            var appCommandLine = await TestServices.Shell.GetRequiredGlobalServiceAsync<SVsAppCommandLine, IVsAppCommandLine>(HangMitigatingCancellationToken);

            Assert.Equal(0, appCommandLine.GetOption("rootSuffix", out var present, out var value));
            Assert.Equal(1, present);
            Assert.Equal("Exp", value);

            Assert.Equal("Exp", Environment.GetEnvironmentVariable("VSROOTSUFFIX"));
        }

        [IdeFact(RootSuffix = "RoslynExp")]
        public async Task TestRoslynExperimentalInstance()
        {
            var appCommandLine = await TestServices.Shell.GetRequiredGlobalServiceAsync<SVsAppCommandLine, IVsAppCommandLine>(HangMitigatingCancellationToken);

            Assert.Equal(0, appCommandLine.GetOption("rootSuffix", out var present, out var value));
            Assert.Equal(1, present);
            Assert.Equal("RoslynExp", value);

            Assert.Equal("RoslynExp", Environment.GetEnvironmentVariable("VSROOTSUFFIX"));
        }

        [IdeFact(EnvironmentVariables = new[] { "CustomKey1=CustomValue", "CustomKey2=A=B;C", "CustomEmptyKey=" })]
        public void TestLaunchWithCustomEnvironmentVariable()
        {
            Assert.Equal("CustomValue", Environment.GetEnvironmentVariable("CustomKey1"));
            Assert.Equal("A=B;C", Environment.GetEnvironmentVariable("CustomKey2"));
            Assert.Null(Environment.GetEnvironmentVariable("CustomEmptyKey"));
        }
    }
}
