// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationTextRangeArray : AutomationRetryWrapper<IUIAutomationTextRangeArray>, IUIAutomationTextRangeArray
    {
        public UIAutomationTextRangeArray(IUIAutomationTextRangeArray automationObject)
            : base(automationObject)
        {
        }

        public int Length => Retry(obj => obj.Length);

        public IUIAutomationTextRange GetElement(int index) => Retry(obj => obj.GetElement(index));
    }
}
