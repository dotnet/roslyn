// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class Dialog_InProc2 : InProcComponent2
    {
        public Dialog_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task VerifyOpenAsync(string dialogName)
        {
            throw new NotImplementedException();
            //// FindDialog will wait until the dialog is open, so the return value is unused.
            //DialogHelpers.FindDialogByName(GetMainWindowHWnd(), dialogName, isOpen: true, CancellationToken.None);

            //// Wait for application idle to ensure the dialog is fully initialized
            //VisualStudioInstance.WaitForApplicationIdle(CancellationToken.None);
        }

        public async Task VerifyClosedAsync(string dialogName)
        {
            throw new NotImplementedException();
            //// FindDialog will wait until the dialog is closed, so the return value is unused.
            //DialogHelpers.FindDialogByName(GetMainWindowHWnd(), dialogName, isOpen: false, CancellationToken.None);
        }

        public async Task ClickAsync(string dialogName, string buttonName)
        {
            throw new NotImplementedException();
            //DialogHelpers.PressButtonWithNameFromDialogWithName(GetMainWindowHWnd(), dialogName, buttonName);
        }
    }
}
