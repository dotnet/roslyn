// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationTreeWalker : AutomationRetryWrapper<IUIAutomationTreeWalker>, IUIAutomationTreeWalker
    {
        public UIAutomationTreeWalker(IUIAutomationTreeWalker automationObject)
            : base(automationObject)
        {
        }

        public IUIAutomationCondition condition => Retry(obj => obj.condition);

        public IUIAutomationElement GetParentElement(IUIAutomationElement element) => Retry(obj => obj.GetParentElement(AutomationRetryWrapper.Unwrap(element)));

        public IUIAutomationElement GetFirstChildElement(IUIAutomationElement element) => Retry(obj => obj.GetFirstChildElement(AutomationRetryWrapper.Unwrap(element)));

        public IUIAutomationElement GetLastChildElement(IUIAutomationElement element) => Retry(obj => obj.GetLastChildElement(AutomationRetryWrapper.Unwrap(element)));

        public IUIAutomationElement GetNextSiblingElement(IUIAutomationElement element) => Retry(obj => obj.GetNextSiblingElement(AutomationRetryWrapper.Unwrap(element)));

        public IUIAutomationElement GetPreviousSiblingElement(IUIAutomationElement element) => Retry(obj => obj.GetPreviousSiblingElement(AutomationRetryWrapper.Unwrap(element)));

        public IUIAutomationElement NormalizeElement(IUIAutomationElement element) => Retry(obj => obj.NormalizeElement(AutomationRetryWrapper.Unwrap(element)));

        public IUIAutomationElement GetParentElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cacheRequest)
            => Retry(obj => obj.GetParentElementBuildCache(AutomationRetryWrapper.Unwrap(element), AutomationRetryWrapper.Unwrap(cacheRequest)));

        public IUIAutomationElement GetFirstChildElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cacheRequest)
            => Retry(obj => obj.GetFirstChildElementBuildCache(AutomationRetryWrapper.Unwrap(element), AutomationRetryWrapper.Unwrap(cacheRequest)));

        public IUIAutomationElement GetLastChildElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cacheRequest)
            => Retry(obj => obj.GetLastChildElementBuildCache(AutomationRetryWrapper.Unwrap(element), AutomationRetryWrapper.Unwrap(cacheRequest)));

        public IUIAutomationElement GetNextSiblingElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cacheRequest)
            => Retry(obj => obj.GetNextSiblingElementBuildCache(AutomationRetryWrapper.Unwrap(element), AutomationRetryWrapper.Unwrap(cacheRequest)));

        public IUIAutomationElement GetPreviousSiblingElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cacheRequest)
            => Retry(obj => obj.GetPreviousSiblingElementBuildCache(AutomationRetryWrapper.Unwrap(element), AutomationRetryWrapper.Unwrap(cacheRequest)));

        public IUIAutomationElement NormalizeElementBuildCache(IUIAutomationElement element, IUIAutomationCacheRequest cacheRequest)
            => Retry(obj => obj.NormalizeElementBuildCache(AutomationRetryWrapper.Unwrap(element), AutomationRetryWrapper.Unwrap(cacheRequest)));
    }
}
