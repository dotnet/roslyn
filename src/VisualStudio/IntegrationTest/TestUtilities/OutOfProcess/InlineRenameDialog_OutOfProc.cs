// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using WindowsInput.Native;

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
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Rename);
        }

        public void ToggleIncludeComments()
        {
            VisualStudioInstance.Editor.SendKeys(new KeyPress(VirtualKeyCode.VK_C, VirtualKeyCode.MENU));
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Rename);
        }

        public void ToggleIncludeStrings()
        {
            VisualStudioInstance.Editor.SendKeys(new KeyPress(VirtualKeyCode.VK_S, VirtualKeyCode.MENU));
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Rename);
        }

        public void ToggleIncludeOverloads()
        {
            VisualStudioInstance.Editor.SendKeys(new KeyPress(VirtualKeyCode.VK_O, VirtualKeyCode.MENU));
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Rename);
        }
    }
}
