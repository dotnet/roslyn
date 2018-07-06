// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class TestServices
    {
        protected TestServices(JoinableTaskFactory joinableTaskFactory)
        {
            JoinableTaskFactory = joinableTaskFactory;

            VisualStudio = new VisualStudio_InProc2(this);
            SolutionExplorer = new SolutionExplorer_InProc2(this);
            Workspace = new VisualStudioWorkspace_InProc2(this);
            SendKeys = new SendKeys_InProc2(this);
            Debugger = new Debugger_InProc2(this);
            Dialog = new Dialog_InProc2(this);
            LocalsWindow = new LocalsWindow_InProc2(this);
            InteractiveWindow = new CSharpInteractiveWindow_InProc2(this);

            Editor = new Editor_InProc2(this);
            EncapsulateField = new EncapsulateField_InProc2(this);
            ErrorList = new ErrorList_InProc2(this);
            PreviewChangesDialog = new PreviewChangesDialog_InProc2(this);

            ChangeSignatureDialog = new ChangeSignatureDialog_InProc2(this);
        }

        public JoinableTaskFactory JoinableTaskFactory
        {
            get;
        }

        public ChangeSignatureDialog_InProc2 ChangeSignatureDialog
        {
            get;
        }

        public Debugger_InProc2 Debugger
        {
            get;
        }

        public Dialog_InProc2 Dialog
        {
            get;
        }

        public Editor_InProc2 Editor
        {
            get;
        }

        public EncapsulateField_InProc2 EncapsulateField
        {
            get;
        }

        public ErrorList_InProc2 ErrorList
        {
            get;
        }

        public CSharpInteractiveWindow_InProc2 InteractiveWindow
        {
            get;
        }

        public LocalsWindow_InProc2 LocalsWindow
        {
            get;
        }

        public PreviewChangesDialog_InProc2 PreviewChangesDialog
        {
            get;
        }

        public SendKeys_InProc2 SendKeys
        {
            get;
        }

        public SolutionExplorer_InProc2 SolutionExplorer
        {
            get;
        }

        public VisualStudio_InProc2 VisualStudio
        {
            get;
        }

        public VisualStudioWorkspace_InProc2 Workspace
        {
            get;
        }

        public static async Task<TestServices> CreateAsync(JoinableTaskFactory joinableTaskFactory)
        {
            var services = new TestServices(joinableTaskFactory);
            await services.InitializeAsync();
            return services;
        }

        protected virtual async Task InitializeAsync()
        {
            await Workspace.InitializeAsync();
        }
    }
}
