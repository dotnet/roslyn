// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationValuePattern : AutomationRetryWrapper<IUIAutomationValuePattern>, IUIAutomationValuePattern
    {
        public UIAutomationValuePattern(IUIAutomationValuePattern automationObject)
            : base(automationObject)
        {
        }

        public string CurrentValue => Retry(obj => obj.CurrentValue);

        public int CurrentIsReadOnly => Retry(obj => obj.CurrentIsReadOnly);

        public string CachedValue => Retry(obj => obj.CachedValue);

        public int CachedIsReadOnly => Retry(obj => obj.CachedIsReadOnly);

        public void SetValue(string val) => Retry(obj => obj.SetValue(val));
    }
}
