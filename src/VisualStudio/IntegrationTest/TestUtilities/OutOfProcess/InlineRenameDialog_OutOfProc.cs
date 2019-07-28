// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class InlineRenameDialog_OutOfProc : OutOfProcComponent
    {
        private const string ChangeSignatureDialogAutomationId = "InlineRenameDialog";

        public string ValidRenameTag => RenameFieldBackgroundAndBorderTag.TagId;

        public InlineRenameDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
        }

        public void Invoke()
        {
            VisualStudioInstance.ExecuteCommand("Refactor.Rename");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Rename);
        }

        public void ToggleIncludeComments()
        {
            VisualStudioInstance.Editor.SendKeys(new KeyPress(VirtualKey.C, ShiftState.Alt));
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Rename);
        }

        public void ToggleIncludeStrings()
        {
            VisualStudioInstance.Editor.SendKeys(new KeyPress(VirtualKey.S, ShiftState.Alt));
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Rename);
        }

        public void ToggleIncludeOverloads()
        {
            VisualStudioInstance.Editor.SendKeys(new KeyPress(VirtualKey.O, ShiftState.Alt));
            VisualStudioInstance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Rename);
        }
    }
}
