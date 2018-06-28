// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2;
using Microsoft.VisualStudio.Threading;
using Xunit;
using ServiceProvider = Microsoft.VisualStudio.Shell.ServiceProvider;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [CaptureTestName]
    public abstract class AbstractIdeIntegrationTest : IAsyncLifetime, IDisposable
    {
        protected readonly string ProjectName = "TestProj";
        protected readonly string SolutionName = "TestSolution";

        private JoinableTaskContext _joinableTaskContext;
        private JoinableTaskCollection _joinableTaskCollection;
        private JoinableTaskFactory _joinableTaskFactory;

        protected AbstractIdeIntegrationTest()
        {
            var componentModel = (IComponentModel)ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel));
            JoinableTaskContext = componentModel.GetExtensions<JoinableTaskContext>().SingleOrDefault() ?? new JoinableTaskContext();
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

        protected TestServices VisualStudioInstance => TestServices;

        protected ChangeSignatureDialog_InProc2 ChangeSignatureDialog => TestServices.ChangeSignatureDialog;

        protected Editor_InProc2 Editor => TestServices.Editor;

        protected ErrorList_InProc2 ErrorList => TestServices.ErrorList;

        protected SendKeys_InProc2 SendKeys => TestServices.SendKeys;

        protected SolutionExplorer_InProc2 SolutionExplorer => TestServices.SolutionExplorer;

        protected VisualStudioWorkspace_InProc2 Workspace => TestServices.Workspace;

        public virtual async Task InitializeAsync()
        {
            TestServices = await CreateTestServicesAsync();

            await CleanUpAsync();
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
        }

        protected virtual async Task<TestServices> CreateTestServicesAsync()
        {
            return await TestServices.CreateAsync(JoinableTaskFactory);
        }

        protected virtual async Task CleanUpAsync()
        {
            await VisualStudioInstance.Workspace.CleanUpWaitingServiceAsync();
            await VisualStudioInstance.Workspace.CleanUpWorkspaceAsync();
            await VisualStudioInstance.SolutionExplorer.CleanUpOpenSolutionAsync();
            await VisualStudioInstance.Workspace.WaitForAllAsyncOperationsAsync();

            // Close any windows leftover from previous (failed) tests
#if false
            VisualStudioInstance.InteractiveWindow.CloseInteractiveWindow();
            VisualStudioInstance.ObjectBrowserWindow.CloseWindow();
#endif
            await VisualStudioInstance.ChangeSignatureDialog.CloseWindowAsync();
#if false
            VisualStudioInstance.GenerateTypeDialog.CloseWindow();
            VisualStudioInstance.ExtractInterfaceDialog.CloseWindow();
#endif
        }
    }
}
