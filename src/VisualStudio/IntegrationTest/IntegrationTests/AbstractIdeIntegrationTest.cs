// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.Threading;
using Xunit;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [CaptureTestName]
    public abstract class AbstractIdeIntegrationTest : IAsyncLifetime, IDisposable
    {
        protected readonly string ProjectName = "TestProj";
        protected readonly string SolutionName = "TestSolution";

        private readonly CancellationTokenSource _watchdogCompletionTokenSource;

        private JoinableTaskContext _joinableTaskContext;
        private JoinableTaskCollection _joinableTaskCollection;
        private JoinableTaskFactory _joinableTaskFactory;

        private CancellationTokenSource _hangMitigatingCancellationTokenSource;

        protected AbstractIdeIntegrationTest()
        {
            JoinableTaskContext = ThreadHelper.JoinableTaskContext;

            _hangMitigatingCancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout);
            _watchdogCompletionTokenSource = new CancellationTokenSource();
        }

        protected JoinableTaskContext JoinableTaskContext
        {
            get
            {
                return _joinableTaskContext ?? throw new InvalidOperationException();
            }

            private set
            {
                if (value == _joinableTaskContext)
                {
                    return;
                }

                if (value is null)
                {
                    _joinableTaskContext = null;
                    _joinableTaskCollection = null;
                    _joinableTaskFactory = null;
                }
                else
                {
                    _joinableTaskContext = value;
                    _joinableTaskCollection = value.CreateCollection();
                    _joinableTaskFactory = value.CreateFactory(_joinableTaskCollection);
                }
            }
        }

        protected JoinableTaskFactory JoinableTaskFactory => _joinableTaskFactory ?? throw new InvalidOperationException();

        protected TestServices TestServices
        {
            get;
            private set;
        }

        protected CancellationToken HangMitigatingCancellationToken => _hangMitigatingCancellationTokenSource.Token;

        protected TestServices VisualStudio => TestServices;

        protected ChangeSignatureDialog_InProc2 ChangeSignatureDialog => TestServices.ChangeSignatureDialog;

        protected Editor_InProc2 Editor => TestServices.Editor;

        protected ErrorList_InProc2 ErrorList => TestServices.ErrorList;

        protected SendKeys_InProc2 SendKeys => TestServices.SendKeys;

        protected SolutionExplorer_InProc2 SolutionExplorer => TestServices.SolutionExplorer;

        protected VisualStudioWorkspace_InProc2 Workspace => TestServices.Workspace;

        public virtual async Task InitializeAsync()
        {
            var testName = CaptureTestNameAttribute.CurrentName;
            WatchdogAsync().Forget();

            TestServices = await CreateTestServicesAsync();

            _hangMitigatingCancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout);

            await CleanUpAsync();

            _hangMitigatingCancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout);

            return;

            // Local function
            async Task WatchdogAsync()
            {
                await Task.Delay(TimeSpan.FromMilliseconds(3 * Helper.HangMitigatingTimeout.TotalMilliseconds), _watchdogCompletionTokenSource.Token).ConfigureAwait(false);

                var ex = new Exception($"Terminating test '{GetType().Name}.{testName}' run due to unrecoverable test timeout.");
                InProcessIdeTestAssemblyRunner.SaveScreenshot(ex);

                if (!Debugger.IsAttached)
                {
                    Environment.FailFast(ex.Message);
                }
            }
        }

        public virtual async Task DisposeAsync()
        {
            await _joinableTaskCollection.JoinTillEmptyAsync();
            JoinableTaskContext = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _watchdogCompletionTokenSource.Cancel();
            }
        }

        protected virtual async Task<TestServices> CreateTestServicesAsync()
        {
            return await TestServices.CreateAsync(JoinableTaskFactory);
        }

        protected virtual async Task CleanUpAsync()
        {
            await VisualStudio.Workspace.CleanUpWaitingServiceAsync();
            await VisualStudio.Workspace.CleanUpWorkspaceAsync();
            await VisualStudio.SolutionExplorer.CleanUpOpenSolutionAsync();
            await VisualStudio.Workspace.WaitForAllAsyncOperationsAsync();

            // Close any windows leftover from previous (failed) tests
            await VisualStudio.ChangeSignatureDialog.CloseWindowAsync();
            await VisualStudio.GenerateTypeDialog.CloseWindowAsync();
            await VisualStudio.ExtractInterfaceDialog.CloseWindowAsync();
            await VisualStudio.InteractiveWindow.CloseWindowAsync();
            await VisualStudio.ImmediateWindow.CloseWindowAsync();
            await VisualStudio.ObjectBrowserWindow.CloseWindowAsync();
        }

        protected KeyPress Ctrl(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Ctrl);

        protected KeyPress Shift(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Shift);

        protected KeyPress Alt(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Alt);
    }
}
