// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable disable

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
        public void TestOpenAndCloseIDE()
        {
            Assert.Equal("devenv", Process.GetCurrentProcess().ProcessName);
            var dte = (DTE)ServiceProvider.GetService(typeof(_DTE));
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
        public void TestJoinableTaskFactoryProvidedByTest()
        {
            var taskSchedulerServiceObject = ServiceProvider.GetService(typeof(SVsTaskSchedulerService));
            Assert.NotNull(taskSchedulerServiceObject);

            var taskSchedulerService = taskSchedulerServiceObject as IVsTaskSchedulerService;
            Assert.NotNull(taskSchedulerService);

            var taskSchedulerService2 = taskSchedulerServiceObject as IVsTaskSchedulerService2;
            Assert.Null(taskSchedulerService2);

            Assert.NotNull(JoinableTaskContext);
        }

        [IdeFact(MinVersion = VisualStudioVersion.VS2013)]
        public void TestJoinableTaskFactoryObtainedFromEnvironment()
        {
            var taskSchedulerServiceObject = ServiceProvider.GetService(typeof(SVsTaskSchedulerService));
            Assert.NotNull(taskSchedulerServiceObject);

            var taskSchedulerService = taskSchedulerServiceObject as IVsTaskSchedulerService2;
            Assert.NotNull(taskSchedulerService);

            Assert.Same(JoinableTaskContext, taskSchedulerService.GetAsyncTaskContext());
        }

        /// <summary>
        /// ⚠️ Running this test locally will reset the setting for the non-experimental instance.
        /// </summary>
        [IdeFact(RootSuffix = "")]
        public void TestStandardInstance()
        {
            var appCommandLine = (IVsAppCommandLine)ServiceProvider.GetService(typeof(SVsAppCommandLine));
            Assumes.Present(appCommandLine);

            Assert.Equal(0, appCommandLine.GetOption("rootSuffix", out var present, out var value));
            Assert.Equal(0, present);
            Assert.Null(value);

            Assert.Null(Environment.GetEnvironmentVariable("VSROOTSUFFIX"));
        }

        [IdeFact]
        public void TestDefaultExperimentalInstance1()
        {
            var appCommandLine = (IVsAppCommandLine)ServiceProvider.GetService(typeof(SVsAppCommandLine));
            Assumes.Present(appCommandLine);

            Assert.Equal(0, appCommandLine.GetOption("rootSuffix", out var present, out var value));
            Assert.Equal(1, present);
            Assert.Equal("Exp", value);

            Assert.Equal("Exp", Environment.GetEnvironmentVariable("VSROOTSUFFIX"));
        }

        [IdeFact(RootSuffix = null)]
        public void TestDefaultExperimentalInstance2()
        {
            var appCommandLine = (IVsAppCommandLine)ServiceProvider.GetService(typeof(SVsAppCommandLine));
            Assumes.Present(appCommandLine);

            Assert.Equal(0, appCommandLine.GetOption("rootSuffix", out var present, out var value));
            Assert.Equal(1, present);
            Assert.Equal("Exp", value);

            Assert.Equal("Exp", Environment.GetEnvironmentVariable("VSROOTSUFFIX"));
        }

        [IdeFact(RootSuffix = "Exp")]
        public void TestExperimentalInstance()
        {
            var appCommandLine = (IVsAppCommandLine)ServiceProvider.GetService(typeof(SVsAppCommandLine));
            Assumes.Present(appCommandLine);

            Assert.Equal(0, appCommandLine.GetOption("rootSuffix", out var present, out var value));
            Assert.Equal(1, present);
            Assert.Equal("Exp", value);

            Assert.Equal("Exp", Environment.GetEnvironmentVariable("VSROOTSUFFIX"));
        }

        [IdeFact(RootSuffix = "RoslynExp")]
        public void TestRoslynExperimentalInstance()
        {
            var appCommandLine = (IVsAppCommandLine)ServiceProvider.GetService(typeof(SVsAppCommandLine));
            Assumes.Present(appCommandLine);

            Assert.Equal(0, appCommandLine.GetOption("rootSuffix", out var present, out var value));
            Assert.Equal(1, present);
            Assert.Equal("RoslynExp", value);

            Assert.Equal("RoslynExp", Environment.GetEnvironmentVariable("VSROOTSUFFIX"));
        }
    }
}
