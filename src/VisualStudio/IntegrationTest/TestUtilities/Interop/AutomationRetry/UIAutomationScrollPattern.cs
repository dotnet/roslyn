// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationScrollPattern : AutomationRetryWrapper<IUIAutomationScrollPattern>, IUIAutomationScrollPattern
    {
        public UIAutomationScrollPattern(IUIAutomationScrollPattern automationObject)
            : base(automationObject)
        {
        }

        public double CurrentHorizontalScrollPercent => Retry(obj => obj.CurrentHorizontalScrollPercent);

        public double CurrentVerticalScrollPercent => Retry(obj => obj.CurrentVerticalScrollPercent);

        public double CurrentHorizontalViewSize => Retry(obj => obj.CurrentHorizontalViewSize);

        public double CurrentVerticalViewSize => Retry(obj => obj.CurrentVerticalViewSize);

        public int CurrentHorizontallyScrollable => Retry(obj => obj.CurrentHorizontallyScrollable);

        public int CurrentVerticallyScrollable => Retry(obj => obj.CurrentVerticallyScrollable);

        public double CachedHorizontalScrollPercent => Retry(obj => obj.CachedHorizontalScrollPercent);

        public double CachedVerticalScrollPercent => Retry(obj => obj.CachedVerticalScrollPercent);

        public double CachedHorizontalViewSize => Retry(obj => obj.CachedHorizontalViewSize);

        public double CachedVerticalViewSize => Retry(obj => obj.CachedVerticalViewSize);

        public int CachedHorizontallyScrollable => Retry(obj => obj.CachedHorizontallyScrollable);

        public int CachedVerticallyScrollable => Retry(obj => obj.CachedVerticallyScrollable);

        public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount) => Retry(obj => obj.Scroll(horizontalAmount, verticalAmount));

        public void SetScrollPercent(double horizontalPercent, double verticalPercent) => Retry(obj => obj.SetScrollPercent(horizontalPercent, verticalPercent));
    }
}
