// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationBoolCondition : AutomationRetryWrapper<IUIAutomationBoolCondition>, IUIAutomationBoolCondition
    {
        public UIAutomationBoolCondition(IUIAutomationBoolCondition automationObject)
            : base(automationObject)
        {
        }

        public int BooleanValue => Retry(obj => obj.BooleanValue);
    }
}
