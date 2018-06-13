// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationSpreadsheetPattern : AutomationRetryWrapper<IUIAutomationSpreadsheetPattern>, IUIAutomationSpreadsheetPattern
    {
        public UIAutomationSpreadsheetPattern(IUIAutomationSpreadsheetPattern automationObject)
            : base(automationObject)
        {
        }

        public IUIAutomationElement GetItemByName(string name) => Retry(obj => obj.GetItemByName(name));
    }
}
