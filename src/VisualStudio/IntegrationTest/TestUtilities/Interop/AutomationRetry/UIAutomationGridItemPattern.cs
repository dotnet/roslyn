// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationGridItemPattern : AutomationRetryWrapper<IUIAutomationGridItemPattern>, IUIAutomationGridItemPattern
    {
        public UIAutomationGridItemPattern(IUIAutomationGridItemPattern automationObject)
            : base(automationObject)
        {
        }

        public IUIAutomationElement CurrentContainingGrid => Retry(obj => obj.CurrentContainingGrid);

        public int CurrentRow => Retry(obj => obj.CurrentRow);

        public int CurrentColumn => Retry(obj => obj.CurrentColumn);

        public int CurrentRowSpan => Retry(obj => obj.CurrentRowSpan);

        public int CurrentColumnSpan => Retry(obj => obj.CurrentColumnSpan);

        public IUIAutomationElement CachedContainingGrid => Retry(obj => obj.CachedContainingGrid);

        public int CachedRow => Retry(obj => obj.CachedRow);

        public int CachedColumn => Retry(obj => obj.CachedColumn);

        public int CachedRowSpan => Retry(obj => obj.CachedRowSpan);

        public int CachedColumnSpan => Retry(obj => obj.CachedColumnSpan);
    }
}
