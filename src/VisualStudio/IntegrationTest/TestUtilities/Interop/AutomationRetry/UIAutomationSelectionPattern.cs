// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationSelectionPattern : AutomationRetryWrapper<IUIAutomationSelectionPattern>, IUIAutomationSelectionPattern
    {
        public UIAutomationSelectionPattern(IUIAutomationSelectionPattern automationObject)
            : base(automationObject)
        {
        }

        public int CurrentCanSelectMultiple => Retry(obj => obj.CurrentCanSelectMultiple);

        public int CurrentIsSelectionRequired => Retry(obj => obj.CurrentIsSelectionRequired);

        public int CachedCanSelectMultiple => Retry(obj => obj.CachedCanSelectMultiple);

        public int CachedIsSelectionRequired => Retry(obj => obj.CachedIsSelectionRequired);

        public IUIAutomationElementArray GetCurrentSelection() => Retry(obj => obj.GetCurrentSelection());

        public IUIAutomationElementArray GetCachedSelection() => Retry(obj => obj.GetCachedSelection());
    }
}
