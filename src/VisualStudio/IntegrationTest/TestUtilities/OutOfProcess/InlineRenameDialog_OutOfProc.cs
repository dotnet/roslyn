// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class InlineRenameDialog_OutOfProc : OutOfProcComponent
    {
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
