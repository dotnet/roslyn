// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows.Automation;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;


namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class EncapsulateField_OutOfProc : OutOfProcComponent
    {
        public string DialogName = "Preview Changes - Encapsulate Field";

        public EncapsulateField_OutOfProc(VisualStudioInstance visualStudioInstance) : base(visualStudioInstance)
        {
        }

        public void Invoke()
            => VisualStudioInstance.Editor.SendKeys(new KeyPress(VirtualKey.R, ShiftState.Ctrl), new KeyPress(VirtualKey.E, ShiftState.Ctrl));
    }
}
