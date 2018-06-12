// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationWindowPattern : AutomationRetryWrapper<IUIAutomationWindowPattern>, IUIAutomationWindowPattern
    {
        public UIAutomationWindowPattern(IUIAutomationWindowPattern automationObject)
            : base(automationObject)
        {
        }

        public int CurrentCanMaximize => Retry(obj => obj.CurrentCanMaximize);

        public int CurrentCanMinimize => Retry(obj => obj.CurrentCanMinimize);

        public int CurrentIsModal => Retry(obj => obj.CurrentIsModal);

        public int CurrentIsTopmost => Retry(obj => obj.CurrentIsTopmost);

        public WindowVisualState CurrentWindowVisualState => Retry(obj => obj.CurrentWindowVisualState);

        public WindowInteractionState CurrentWindowInteractionState => Retry(obj => obj.CurrentWindowInteractionState);

        public int CachedCanMaximize => Retry(obj => obj.CachedCanMaximize);

        public int CachedCanMinimize => Retry(obj => obj.CachedCanMinimize);

        public int CachedIsModal => Retry(obj => obj.CachedIsModal);

        public int CachedIsTopmost => Retry(obj => obj.CachedIsTopmost);

        public WindowVisualState CachedWindowVisualState => Retry(obj => obj.CachedWindowVisualState);

        public WindowInteractionState CachedWindowInteractionState => Retry(obj => obj.CachedWindowInteractionState);

        public void Close() => Retry(obj => obj.Close());

        public int WaitForInputIdle(int milliseconds) => Retry(obj => obj.WaitForInputIdle(milliseconds));

        public void SetWindowVisualState(WindowVisualState state) => Retry(obj => obj.SetWindowVisualState(state));
    }
}
