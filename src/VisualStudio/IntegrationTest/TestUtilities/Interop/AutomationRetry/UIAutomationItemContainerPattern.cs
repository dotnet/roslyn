// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationItemContainerPattern : AutomationRetryWrapper<IUIAutomationItemContainerPattern>, IUIAutomationItemContainerPattern
    {
        public UIAutomationItemContainerPattern(IUIAutomationItemContainerPattern automationObject)
            : base(automationObject)
        {
        }

        public IUIAutomationElement FindItemByProperty(IUIAutomationElement pStartAfter, int propertyId, object value)
            => Retry(obj => obj.FindItemByProperty(AutomationRetryWrapper.Unwrap(pStartAfter), propertyId, value));
    }
}
