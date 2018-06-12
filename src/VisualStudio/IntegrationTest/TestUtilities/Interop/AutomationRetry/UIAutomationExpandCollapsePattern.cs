// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationExpandCollapsePattern : AutomationRetryWrapper<IUIAutomationExpandCollapsePattern>, IUIAutomationExpandCollapsePattern
    {
        public UIAutomationExpandCollapsePattern(IUIAutomationExpandCollapsePattern automationObject)
            : base(automationObject)
        {
        }

        public ExpandCollapseState CurrentExpandCollapseState => Retry(obj => obj.CurrentExpandCollapseState);

        public ExpandCollapseState CachedExpandCollapseState => Retry(obj => obj.CachedExpandCollapseState);

        public void Expand() => Retry(obj => obj.Expand());

        public void Collapse() => Retry(obj => obj.Collapse());
    }
}
