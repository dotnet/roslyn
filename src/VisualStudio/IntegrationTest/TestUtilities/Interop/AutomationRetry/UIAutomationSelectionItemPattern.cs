// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationSelectionItemPattern : AutomationRetryWrapper<IUIAutomationSelectionItemPattern>, IUIAutomationSelectionItemPattern
    {
        public UIAutomationSelectionItemPattern(IUIAutomationSelectionItemPattern automationObject)
            : base(automationObject)
        {
        }

        public int CurrentIsSelected => Retry(obj => obj.CurrentIsSelected);

        public IUIAutomationElement CurrentSelectionContainer => Retry(obj => obj.CurrentSelectionContainer);

        public int CachedIsSelected => Retry(obj => obj.CachedIsSelected);

        public IUIAutomationElement CachedSelectionContainer => Retry(obj => obj.CachedSelectionContainer);

        public void Select() => Retry(obj => obj.Select());

        public void AddToSelection() => Retry(obj => obj.AddToSelection());

        public void RemoveFromSelection() => Retry(obj => obj.RemoveFromSelection());
    }
}
