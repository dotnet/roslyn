// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class GenerateTypeDialog_OutOfProc : OutOfProcComponent
    {
        private readonly GenerateTypeDialog_InProc _inProc;

        public GenerateTypeDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<GenerateTypeDialog_InProc>(visualStudioInstance);
        }

        public void VerifyOpen()
            => _inProc.VerifyOpen();

        public void VerifyClosed()
            => _inProc.VerifyClosed();

        public bool CloseWindow()
            => _inProc.CloseWindow();

        public void SetAccessibility(string accessibility)
            => _inProc.SetAccessibility(accessibility);

        public void SetKind(string kind)
            => _inProc.SetKind(kind);

        public void SetTargetProject(string projectName)
            => _inProc.SetTargetProject(projectName);

        public void SetTargetFileToNewName(string newFileName)
            => _inProc.SetTargetFileToNewName(newFileName);

        /// <summary>
        /// Clicks the "OK" button and waits for the related Code Action to complete.
        /// </summary>
        public void ClickOK()
        {
            _inProc.ClickOK();
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.LightBulb);
        }

        /// <summary>
        /// Clicks the "Cancel" button and waits for the related Code Action to complete.
        /// </summary>
        public void ClickCancel()
        {
            _inProc.ClickCancel();
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.LightBulb);
        }

        public string[] GetNewFileComboBoxItems()
            => _inProc.GetNewFileComboBoxItems();
    }
}
