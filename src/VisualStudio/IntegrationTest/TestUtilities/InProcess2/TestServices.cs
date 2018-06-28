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

            SolutionExplorer = new SolutionExplorer_InProc2(this);
            Workspace = new VisualStudioWorkspace_InProc2(this);
            SendKeys = new SendKeys_InProc2(this);

            Editor = new Editor_InProc2(this);
            ErrorList = new ErrorList_InProc2(this);

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

        public Editor_InProc2 Editor
        {
            get;
        }

        public ErrorList_InProc2 ErrorList
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
