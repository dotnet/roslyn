// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class EncapsulateField_InProc2 : InProcComponent2
    {
        public EncapsulateField_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        public string DialogName => "Preview Changes - Encapsulate Field";

        public async Task InvokeAsync(bool waitForLightBulbSession = true)
        {
            if (waitForLightBulbSession)
            {
                await TestServices.Editor.WaitForLightBulbSessionAsync();
            }

            await TestServices.Editor.SendKeysAsync(new KeyPress(VirtualKey.R, ShiftState.Ctrl), new KeyPress(VirtualKey.E, ShiftState.Ctrl));
        }
    }
}
