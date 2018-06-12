// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationAndCondition : AutomationRetryWrapper<IUIAutomationAndCondition>, IUIAutomationAndCondition
    {
        public UIAutomationAndCondition(IUIAutomationAndCondition automationObject)
            : base(automationObject)
        {
        }

        public int ChildCount => Retry(obj => obj.ChildCount);

        public void GetChildrenAsNativeArray(IntPtr childArray, out int childArrayCount)
        {
            var childArrayCountResult = 0;
            Retry(obj => obj.GetChildrenAsNativeArray(childArray, out childArrayCountResult));
            childArrayCount = childArrayCountResult;
        }

        public IUIAutomationCondition[] GetChildren() => Retry(obj => obj.GetChildren());
    }
}
