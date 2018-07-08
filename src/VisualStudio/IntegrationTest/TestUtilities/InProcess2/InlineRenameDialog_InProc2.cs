// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class InlineRenameDialog_InProc2 : InProcComponent2
    {
        public InlineRenameDialog_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        public string ValidRenameTag => RenameFieldBackgroundAndBorderTag.TagId;

        public async Task InvokeAsync()
        {
            await ExecuteCommandAsync(WellKnownCommandNames.Refactor_Rename);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Rename);
        }

        public async Task ToggleIncludeCommentsAsync()
        {
            await TestServices.Editor.SendKeysAsync(new KeyPress(VirtualKey.C, ShiftState.Alt));
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Rename);
        }

        public async Task ToggleIncludeStringsAsync()
        {
            await TestServices.Editor.SendKeysAsync(new KeyPress(VirtualKey.S, ShiftState.Alt));
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Rename);
        }

        public async Task ToggleIncludeOverloadsAsync()
        {
            await TestServices.Editor.SendKeysAsync(new KeyPress(VirtualKey.O, ShiftState.Alt));
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Rename);
        }            
    }
}
