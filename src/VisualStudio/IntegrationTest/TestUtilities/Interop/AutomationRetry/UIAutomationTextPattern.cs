// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationTextPattern : AutomationRetryWrapper<IUIAutomationTextPattern>, IUIAutomationTextPattern
    {
        public UIAutomationTextPattern(IUIAutomationTextPattern automationObject)
            : base(automationObject)
        {
        }

        public IUIAutomationTextRange DocumentRange => Retry(obj => obj.DocumentRange);

        public SupportedTextSelection SupportedTextSelection => Retry(obj => obj.SupportedTextSelection);

        public IUIAutomationTextRange RangeFromPoint(tagPOINT pt) => Retry(obj => obj.RangeFromPoint(pt));

        public IUIAutomationTextRange RangeFromChild(IUIAutomationElement child) => Retry(obj => obj.RangeFromChild(AutomationRetryWrapper.Unwrap(child)));

        public IUIAutomationTextRangeArray GetSelection() => Retry(obj => obj.GetSelection());

        public IUIAutomationTextRangeArray GetVisibleRanges() => Retry(obj => obj.GetVisibleRanges());
    }
}
