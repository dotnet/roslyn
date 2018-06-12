// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationRangeValuePattern : AutomationRetryWrapper<IUIAutomationRangeValuePattern>, IUIAutomationRangeValuePattern
    {
        public UIAutomationRangeValuePattern(IUIAutomationRangeValuePattern automationObject)
            : base(automationObject)
        {
        }

        public double CurrentValue => Retry(obj => obj.CurrentValue);

        public int CurrentIsReadOnly => Retry(obj => obj.CurrentIsReadOnly);

        public double CurrentMaximum => Retry(obj => obj.CurrentMaximum);

        public double CurrentMinimum => Retry(obj => obj.CurrentMinimum);

        public double CurrentLargeChange => Retry(obj => obj.CurrentLargeChange);

        public double CurrentSmallChange => Retry(obj => obj.CurrentSmallChange);

        public double CachedValue => Retry(obj => obj.CachedValue);

        public int CachedIsReadOnly => Retry(obj => obj.CachedIsReadOnly);

        public double CachedMaximum => Retry(obj => obj.CachedMaximum);

        public double CachedMinimum => Retry(obj => obj.CachedMinimum);

        public double CachedLargeChange => Retry(obj => obj.CachedLargeChange);

        public double CachedSmallChange => Retry(obj => obj.CachedSmallChange);

        public void SetValue(double val) => Retry(obj => obj.SetValue(val));
    }
}
