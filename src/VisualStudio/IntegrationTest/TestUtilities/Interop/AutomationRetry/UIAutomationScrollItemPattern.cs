// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationScrollItemPattern : AutomationRetryWrapper<IUIAutomationScrollItemPattern>, IUIAutomationScrollItemPattern
    {
        public UIAutomationScrollItemPattern(IUIAutomationScrollItemPattern automationObject)
            : base(automationObject)
        {
        }

        public void ScrollIntoView() => Retry(obj => obj.ScrollIntoView());
    }
}
