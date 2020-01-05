// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Handles interaction with the Extract Interface Dialog.
    /// </summary>
    public class ExtractInterfaceDialog_OutOfProc : OutOfProcComponent
    {
        private readonly ExtractInterfaceDialog_InProc _inProc;

        public ExtractInterfaceDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<ExtractInterfaceDialog_InProc>(visualStudioInstance);
        }

        /// <summary>
        /// Verifies that the Extract Interface dialog is currently open.
        /// </summary>
        public void VerifyOpen()
            => _inProc.VerifyOpen();

        /// <summary>
        /// Verifies that the Extract Interface dialog is currently closed.
        /// </summary>
        public void VerifyClosed()
            => _inProc.VerifyClosed();

        public bool CloseWindow()
            => _inProc.CloseWindow();

        /// <summary>
        /// Clicks the "OK" button and waits for the Extract Interface operation to complete.
        /// </summary>
        public void ClickOK()
        {
            _inProc.ClickOK();
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.LightBulb);
        }

        /// <summary>
        /// Clicks the "Cancel" button and waits for the Extract Interface operation to complete.
        /// </summary>
        public void ClickCancel()
        {
            _inProc.ClickCancel();
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.LightBulb);
        }

        /// <summary>
        /// Returns the name of the generated file that will contain the interface.
        /// </summary>
        public string GetTargetFileName()
            => _inProc.GetTargetFileName();

        /// <summary>
        /// Gets the set of members that are currently checked.
        /// </summary>
        public string[] GetSelectedItems()
            => _inProc.GetSelectedItems();

        /// <summary>
        /// Clicks the "Deselect All" button.
        /// </summary>
        public void ClickDeselectAll()
            => _inProc.ClickDeselectAll();

        /// <summary>
        ///  Clicks the "Select All" button.
        /// </summary>
        public void ClickSelectAll()
            => _inProc.ClickSelectAll();

        public void SelectSameFile()
            => _inProc.SelectSameFile();

        /// <summary>
        /// Clicks the checkbox on the given item, cycling it from on to off or from off to on.
        /// </summary>
        /// <param name="item"></param>
        public void ToggleItem(string item)
            => _inProc.ToggleItem(item);
    }
}
