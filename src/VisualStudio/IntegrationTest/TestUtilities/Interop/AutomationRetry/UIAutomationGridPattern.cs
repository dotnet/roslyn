// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationGridPattern : AutomationRetryWrapper<IUIAutomationGridPattern>, IUIAutomationGridPattern
    {
        public UIAutomationGridPattern(IUIAutomationGridPattern automationObject)
            : base(automationObject)
        {
        }

        public int CurrentRowCount => Retry(obj => obj.CurrentRowCount);

        public int CurrentColumnCount => Retry(obj => obj.CurrentColumnCount);

        public int CachedRowCount => Retry(obj => obj.CachedRowCount);

        public int CachedColumnCount => Retry(obj => obj.CachedColumnCount);

        public IUIAutomationElement GetItem(int row, int column) => Retry(obj => obj.GetItem(row, column));
    }
}
