// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationStructureChangedEventHandler : AutomationRetryWrapper<IUIAutomationStructureChangedEventHandler>, IUIAutomationStructureChangedEventHandler
    {
        public UIAutomationStructureChangedEventHandler(IUIAutomationStructureChangedEventHandler automationObject)
            : base(automationObject)
        {
        }

        public void HandleStructureChangedEvent(IUIAutomationElement sender, StructureChangeType changeType, int[] runtimeId)
            => Retry(obj => obj.HandleStructureChangedEvent(AutomationRetryWrapper.Unwrap(sender), changeType, runtimeId));
    }
}
