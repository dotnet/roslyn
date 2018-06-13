// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationPropertyChangedEventHandler : AutomationRetryWrapper<IUIAutomationPropertyChangedEventHandler>, IUIAutomationPropertyChangedEventHandler
    {
        public UIAutomationPropertyChangedEventHandler(IUIAutomationPropertyChangedEventHandler automationObject)
            : base(automationObject)
        {
        }

        public void HandlePropertyChangedEvent(IUIAutomationElement sender, int propertyId, object newValue)
            => Retry(obj => obj.HandlePropertyChangedEvent(AutomationRetryWrapper.Unwrap(sender), propertyId, newValue));
    }
}
