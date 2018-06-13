// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationTextRange : AutomationRetryWrapper<IUIAutomationTextRange>, IUIAutomationTextRange
    {
        public UIAutomationTextRange(IUIAutomationTextRange automationObject)
            : base(automationObject)
        {
        }

        public IUIAutomationTextRange Clone() => Retry(obj => obj.Clone());

        public int Compare(IUIAutomationTextRange range) => Retry(obj => obj.Compare(AutomationRetryWrapper.Unwrap(range)));

        public int CompareEndpoints(TextPatternRangeEndpoint srcEndPoint, IUIAutomationTextRange range, TextPatternRangeEndpoint targetEndPoint) => Retry(obj => obj.CompareEndpoints(srcEndPoint, AutomationRetryWrapper.Unwrap(range), targetEndPoint));

        public void ExpandToEnclosingUnit(TextUnit TextUnit) => Retry(obj => obj.ExpandToEnclosingUnit(TextUnit));

        public IUIAutomationTextRange FindAttribute(int attr, object val, int backward) => Retry(obj => obj.FindAttribute(attr, AutomationRetryWrapper.Unwrap(val), backward));

        public IUIAutomationTextRange FindText(string text, int backward, int ignoreCase) => Retry(obj => obj.FindText(text, backward, ignoreCase));

        public object GetAttributeValue(int attr) => Retry(obj => obj.GetAttributeValue(attr));

        public double[] GetBoundingRectangles() => Retry(obj => obj.GetBoundingRectangles());

        public IUIAutomationElement GetEnclosingElement() => Retry(obj => obj.GetEnclosingElement());

        public string GetText(int maxLength) => Retry(obj => obj.GetText(maxLength));

        public int Move(TextUnit unit, int count) => Retry(obj => obj.Move(unit, count));

        public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count) => Retry(obj => obj.MoveEndpointByUnit(endpoint, unit, count));

        public void MoveEndpointByRange(TextPatternRangeEndpoint srcEndPoint, IUIAutomationTextRange range, TextPatternRangeEndpoint targetEndPoint) => Retry(obj => obj.MoveEndpointByRange(srcEndPoint, AutomationRetryWrapper.Unwrap(range), targetEndPoint));

        public void Select() => Retry(obj => obj.Select());

        public void AddToSelection() => Retry(obj => obj.AddToSelection());

        public void RemoveFromSelection() => Retry(obj => obj.RemoveFromSelection());

        public void ScrollIntoView(int alignToTop) => Retry(obj => obj.ScrollIntoView(alignToTop));

        public IUIAutomationElementArray GetChildren() => Retry(obj => obj.GetChildren());
    }
}
