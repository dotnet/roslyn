// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationEventHandler : AutomationRetryWrapper<IUIAutomationEventHandler>, IUIAutomationEventHandler
    {
        public UIAutomationEventHandler(IUIAutomationEventHandler automationObject)
            : base(automationObject)
        {
        }

        public void HandleAutomationEvent(IUIAutomationElement sender, int eventId)
            => Retry(obj => obj.HandleAutomationEvent(AutomationRetryWrapper.Unwrap(sender), eventId));
    }
}
