// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationNotCondition : AutomationRetryWrapper<IUIAutomationNotCondition>, IUIAutomationNotCondition
    {
        public UIAutomationNotCondition(IUIAutomationNotCondition automationObject)
            : base(automationObject)
        {
        }

        public IUIAutomationCondition GetChild() => Retry(obj => obj.GetChild());
    }
}
