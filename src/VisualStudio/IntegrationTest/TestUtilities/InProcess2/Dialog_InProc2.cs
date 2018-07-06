// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class Dialog_InProc2 : InProcComponent2
    {
        public Dialog_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task<IntPtr> VerifyOpenAsync(string dialogName, CancellationToken cancellationToken)
        {
            // FindDialogByNameAsync will wait until the dialog is open, so the return value is unused.
            var dialog = await FindDialogByNameAsync(dialogName, isOpen: true, cancellationToken);

            // Wait for application idle to ensure the dialog is fully initialized
            await WaitForApplicationIdleAsync(CancellationToken.None);

            return dialog;
        }

        public async Task VerifyClosedAsync(string dialogName, CancellationToken cancellationToken)
        {
            // FindDialog will wait until the dialog is closed, so the return value is unused.
            await FindDialogByNameAsync(dialogName, isOpen: false, cancellationToken);
        }

        public async Task ClickOKAsync(string dialogName)
        {
            var windowHandle = await FindDialogByNameAsync(dialogName, isOpen: true, new CancellationToken(canceled: true));
            await TestServices.SendKeys.SendAsync(VirtualKey.Enter);
        }

        public async Task ClickCancelAsync(string dialogName)
        {
            var windowHandle = await FindDialogByNameAsync(dialogName, isOpen: true, new CancellationToken(canceled: true));
            await TestServices.SendKeys.SendAsync(VirtualKey.Escape);
        }

        private async Task<IntPtr> FindDialogByNameAsync(string dialogName, bool isOpen, CancellationToken cancellationToken)
        {
            while (true)
            {
                var modalWindow = IntegrationHelper.GetModalWindowFromParentWindow(MainWindowHandle);
                if (modalWindow != IntPtr.Zero)
                {
                    var text = IntegrationHelper.GetTitleForWindow(modalWindow);
                    if (text == dialogName)
                    {
                        if (isOpen)
                        {
                            // We found the dialog of interest
                            return modalWindow;
                        }
                    }
                    else
                    {
                        // This isn't the dialog we're looking for
                        modalWindow = IntPtr.Zero;
                    }
                }

                if (modalWindow == IntPtr.Zero && !isOpen)
                {
                    // No matching dialog was found
                    return IntPtr.Zero;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            }
        }
    }
}
