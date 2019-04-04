// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Handles interaction with the Pick Members Dialog.
    /// </summary>
    public class PickMembersDialog_OutOfProc : OutOfProcComponent
    {
        private readonly PickMembersDialog_InProc _inProc;

        public PickMembersDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<PickMembersDialog_InProc>(visualStudioInstance);
        }

        public bool CloseWindow()
            => _inProc.CloseWindow();

        /// <summary>
        /// Clicks the "Cancel" button and waits for the Pick Members operation to complete.
        /// </summary>
        public void ClickCancel()
        {
            _inProc.ClickCancel();
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.LightBulb);
        }
    }
}
