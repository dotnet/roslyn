// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationCacheRequest : AutomationRetryWrapper<IUIAutomationCacheRequest>, IUIAutomationCacheRequest
    {
        public UIAutomationCacheRequest(IUIAutomationCacheRequest automationObject)
            : base(automationObject)
        {
        }

        public TreeScope TreeScope
        {
            get => Retry(obj => obj.TreeScope);
            set => Retry(obj => obj.TreeScope = value);
        }
        public IUIAutomationCondition TreeFilter
        {
            get => Retry(obj => obj.TreeFilter);
            set => Retry(obj => obj.TreeFilter = AutomationRetryWrapper.Unwrap(value));
        }
        public AutomationElementMode AutomationElementMode
        {
            get => Retry(obj => obj.AutomationElementMode);
            set => Retry(obj => obj.AutomationElementMode = value);
        }

        public void AddProperty(int propertyId) => Retry(obj => obj.AddProperty(propertyId));

        public void AddPattern(int patternId) => Retry(obj => obj.AddPattern(patternId));

        public IUIAutomationCacheRequest Clone() => Retry(obj => obj.Clone());
    }
}
