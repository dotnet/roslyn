// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationFocusChangedEventHandler : AutomationRetryWrapper<IUIAutomationFocusChangedEventHandler>, IUIAutomationFocusChangedEventHandler
    {
        public UIAutomationFocusChangedEventHandler(IUIAutomationFocusChangedEventHandler automationObject)
            : base(automationObject)
        {
        }

        public void HandleFocusChangedEvent(IUIAutomationElement sender)
            => Retry(obj => obj.HandleFocusChangedEvent(AutomationRetryWrapper.Unwrap(sender)));
    }
}
