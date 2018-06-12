// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationMultipleViewPattern : AutomationRetryWrapper<IUIAutomationMultipleViewPattern>, IUIAutomationMultipleViewPattern
    {
        public UIAutomationMultipleViewPattern(IUIAutomationMultipleViewPattern automationObject)
            : base(automationObject)
        {
        }

        public int CurrentCurrentView => Retry(obj => obj.CurrentCurrentView);

        public int CachedCurrentView => Retry(obj => obj.CachedCurrentView);

        public string GetViewName(int view) => Retry(obj => obj.GetViewName(view));

        public void SetCurrentView(int view) => Retry(obj => obj.SetCurrentView(view));

        public int[] GetCurrentSupportedViews() => Retry(obj => obj.GetCurrentSupportedViews());

        public int[] GetCachedSupportedViews() => Retry(obj => obj.GetCachedSupportedViews());
    }
}
