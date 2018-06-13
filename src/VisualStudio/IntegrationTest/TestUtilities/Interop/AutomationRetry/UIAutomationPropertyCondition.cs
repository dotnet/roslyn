// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationPropertyCondition : AutomationRetryWrapper<IUIAutomationPropertyCondition>, IUIAutomationPropertyCondition
    {
        public UIAutomationPropertyCondition(IUIAutomationPropertyCondition automationObject)
            : base(automationObject)
        {
        }

        public int propertyId => Retry(obj => obj.propertyId);

        public object PropertyValue => Retry(obj => obj.PropertyValue);

        public PropertyConditionFlags PropertyConditionFlags => Retry(obj => obj.PropertyConditionFlags);
    }
}
