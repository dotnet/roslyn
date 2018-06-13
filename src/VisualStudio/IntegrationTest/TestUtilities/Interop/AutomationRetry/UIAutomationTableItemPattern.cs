// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationTableItemPattern : AutomationRetryWrapper<IUIAutomationTableItemPattern>, IUIAutomationTableItemPattern
    {
        public UIAutomationTableItemPattern(IUIAutomationTableItemPattern automationObject)
            : base(automationObject)
        {
        }

        public IUIAutomationElementArray GetCurrentRowHeaderItems() => Retry(obj => obj.GetCurrentRowHeaderItems());

        public IUIAutomationElementArray GetCurrentColumnHeaderItems() => Retry(obj => obj.GetCurrentColumnHeaderItems());

        public IUIAutomationElementArray GetCachedRowHeaderItems() => Retry(obj => obj.GetCachedRowHeaderItems());

        public IUIAutomationElementArray GetCachedColumnHeaderItems() => Retry(obj => obj.GetCachedColumnHeaderItems());
    }
}
