// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationTogglePattern : AutomationRetryWrapper<IUIAutomationTogglePattern>, IUIAutomationTogglePattern
    {
        public UIAutomationTogglePattern(IUIAutomationTogglePattern automationObject)
            : base(automationObject)
        {
        }

        public ToggleState CurrentToggleState => Retry(obj => obj.CurrentToggleState);

        public ToggleState CachedToggleState => Retry(obj => obj.CachedToggleState);

        public void Toggle() => Retry(obj => obj.Toggle());
    }
}
