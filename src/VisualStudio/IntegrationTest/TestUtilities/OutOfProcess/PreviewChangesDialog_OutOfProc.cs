﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows.Automation;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;


namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class PreviewChangesDialog_OutOfProc : OutOfProcComponent
    {
        public PreviewChangesDialog_OutOfProc(VisualStudioInstance visualStudioInstance) : base(visualStudioInstance)
        {
        }

        /// <summary>
        /// Verifies that the Preview Changes dialog is showing with the
        /// specified title. The dialog does not have an AutomationId and the 
        /// title can be changed by features, so callers of this method must
        /// specify a title.
        /// </summary>
        /// <param name="expectedTitle"></param>
        public void VerifyOpen(string expectedTitle)
            => DialogHelpers.FindDialogByName(GetMainWindowHWnd(), expectedTitle, isOpen: true);

        public void VerifyClosed(string expectedTitle)
            => DialogHelpers.FindDialogByName(GetMainWindowHWnd(), expectedTitle, isOpen: false);

        public void ClickApply(string expectedTitle)
            => DialogHelpers.PressButtonWithNameFromDialogWithName(GetMainWindowHWnd(), expectedTitle, "Apply"); 

        public void ClickCancel(string expectedTitle)
            => DialogHelpers.PressButtonWithNameFromDialogWithName(GetMainWindowHWnd(), expectedTitle, "Cancel");

        private int GetMainWindowHWnd()
            => VisualStudioInstance.Shell.GetHWnd();
    }
}
