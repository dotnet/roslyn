// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationDragPattern : AutomationRetryWrapper<IUIAutomationDragPattern>, IUIAutomationDragPattern
    {
        public UIAutomationDragPattern(IUIAutomationDragPattern automationObject)
            : base(automationObject)
        {
        }

        public int CurrentIsGrabbed => Retry(obj => obj.CurrentIsGrabbed);

        public int CachedIsGrabbed => Retry(obj => obj.CachedIsGrabbed);

        public string CurrentDropEffect => Retry(obj => obj.CurrentDropEffect);

        public string CachedDropEffect => Retry(obj => obj.CachedDropEffect);

        public string[] CurrentDropEffects => Retry(obj => obj.CurrentDropEffects);

        public string[] CachedDropEffects => Retry(obj => obj.CachedDropEffects);

        public IUIAutomationElementArray GetCurrentGrabbedItems() => Retry(obj => obj.GetCurrentGrabbedItems());

        public IUIAutomationElementArray GetCachedGrabbedItems() => Retry(obj => obj.GetCachedGrabbedItems());
    }
}
