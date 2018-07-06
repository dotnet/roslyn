// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class InlineRenameDialog_InProc2 : InProcComponent2
    {
        private const string ChangeSignatureDialogAutomationId = "InlineRenameDialog";

        public InlineRenameDialog_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        public string ValidRenameTag => RenameFieldBackgroundAndBorderTag.TagId;

#if false
        public void Invoke()
        {
            VisualStudioInstance.ExecuteCommand("Refactor.Rename");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Rename);
        }

        public void ToggleIncludeComments()
        {
            VisualStudioInstance.Editor.SendKeys(new KeyPress(VirtualKey.C, ShiftState.Alt));
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Rename);
        }

        public void ToggleIncludeStrings()
        {
            VisualStudioInstance.Editor.SendKeys(new KeyPress(VirtualKey.S, ShiftState.Alt));
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Rename);
        }

        public void ToggleIncludeOverloads()
        {
            VisualStudioInstance.Editor.SendKeys(new KeyPress(VirtualKey.O, ShiftState.Alt));
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Rename);
        }            
#endif
    }
}
