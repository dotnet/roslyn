// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.Threading;
using Xunit;
using _DTE = EnvDTE._DTE;
using DTE = EnvDTE.DTE;
using ServiceProvider = Microsoft.VisualStudio.Shell.ServiceProvider;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace Roslyn.VisualStudio.IntegrationTests
{
    public class IdeFactTest : AbstractIdeIntegrationTest
    {
        [IdeFact]
        public void TestOpenAndCloseIDE()
        {
            Assert.Equal("devenv", Process.GetCurrentProcess().ProcessName);
            var dte = (DTE)ServiceProvider.GlobalProvider.GetService(typeof(_DTE));
            Assert.NotNull(dte);
        }

        [IdeFact]
        public void TestRunsOnUIThread()
        {
            Assert.True(ThreadHelper.CheckAccess());
        }

        [IdeFact]
        public async Task TestRunsOnUIThreadAsync()
        {
            Assert.True(ThreadHelper.CheckAccess());
            await Task.Yield();
            Assert.True(ThreadHelper.CheckAccess());
        }

        [IdeFact]
        public async Task TestYieldsToWorkAsync()
        {
            Assert.True(ThreadHelper.CheckAccess());
            await Task.Factory.StartNew(
                () => { },
                CancellationToken.None,
                TaskCreationOptions.None,
                new SynchronizationContextTaskScheduler(new DispatcherSynchronizationContext(Application.Current.Dispatcher)));
            Assert.True(ThreadHelper.CheckAccess());
        }

        [IdeFact]
        public async Task TestJoinableTaskFactoryAsync()
        {
            Assert.NotNull(JoinableTaskContext);
            Assert.NotNull(JoinableTaskFactory);
            Assert.True(JoinableTaskContext.IsOnMainThread);

            await TaskScheduler.Default;

            Assert.False(JoinableTaskContext.IsOnMainThread);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            Assert.True(JoinableTaskContext.IsOnMainThread);
        }
    }
}
