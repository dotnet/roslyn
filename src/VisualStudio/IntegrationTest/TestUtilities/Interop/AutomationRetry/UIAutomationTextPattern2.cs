// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationTextPattern2 : AutomationRetryWrapper<IUIAutomationTextPattern2>, IUIAutomationTextPattern2
    {
        public UIAutomationTextPattern2(IUIAutomationTextPattern2 automationObject)
            : base(automationObject)
        {
        }

        public IUIAutomationTextRange DocumentRange => Retry(obj => obj.DocumentRange);

        public SupportedTextSelection SupportedTextSelection => Retry(obj => obj.SupportedTextSelection);

        public IUIAutomationTextRange RangeFromPoint(tagPOINT pt) => Retry(obj => obj.RangeFromPoint(pt));

        public IUIAutomationTextRange RangeFromChild(IUIAutomationElement child) => Retry(obj => obj.RangeFromChild(AutomationRetryWrapper.Unwrap(child)));

        public IUIAutomationTextRangeArray GetSelection() => Retry(obj => obj.GetSelection());

        public IUIAutomationTextRangeArray GetVisibleRanges() => Retry(obj => obj.GetVisibleRanges());

        public IUIAutomationTextRange RangeFromAnnotation(IUIAutomationElement annotation) => Retry(obj => obj.RangeFromAnnotation(AutomationRetryWrapper.Unwrap(annotation)));

        public IUIAutomationTextRange GetCaretRange(out int isActive)
        {
            var isActiveResult = 0;
            var result = Retry(obj => obj.GetCaretRange(out isActiveResult));
            isActive = isActiveResult;
            return result;
        }
    }
}
