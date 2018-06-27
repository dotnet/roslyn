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
    [Collection(nameof(SharedIntegrationHostFixture))]
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

            SolutionExplorer = new SolutionExplorer_InProc2(JoinableTaskFactory);
            Workspace = new VisualStudioWorkspace_InProc2(JoinableTaskFactory);
            SendKeys = new SendKeys_InProc2(JoinableTaskFactory);

            Editor = new Editor_InProc2(JoinableTaskFactory, Workspace, SendKeys);
            ErrorList = new ErrorList_InProc2(JoinableTaskFactory, Workspace);
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

        protected Editor_InProc2 Editor
        {
            get;
        }

        protected ErrorList_InProc2 ErrorList
        {
            get;
        }

        protected SendKeys_InProc2 SendKeys
        {
            get;
        }

        protected SolutionExplorer_InProc2 SolutionExplorer
        {
            get;
        }

        protected VisualStudioWorkspace_InProc2 Workspace
        {
            get;
        }

        public virtual async Task InitializeAsync()
        {
            await Workspace.InitializeAsync();

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

        protected virtual async Task CleanUpAsync()
        {
            await Workspace.CleanUpWaitingServiceAsync();
            await Workspace.CleanUpWorkspaceAsync();
            await SolutionExplorer.CleanUpOpenSolutionAsync();
            await Workspace.WaitForAllAsyncOperationsAsync();

            // Close any windows leftover from previous (failed) tests
#if false
            InteractiveWindow.CloseInteractiveWindow();
            ObjectBrowserWindow.CloseWindow();
            ChangeSignatureDialog.CloseWindow();
            GenerateTypeDialog.CloseWindow();
            ExtractInterfaceDialog.CloseWindow();
#endif
        }
    }
}
