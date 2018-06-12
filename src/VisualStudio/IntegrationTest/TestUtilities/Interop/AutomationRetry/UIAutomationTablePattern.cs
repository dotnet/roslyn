// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationTablePattern : AutomationRetryWrapper<IUIAutomationTablePattern>, IUIAutomationTablePattern
    {
        public UIAutomationTablePattern(IUIAutomationTablePattern automationObject)
            : base(automationObject)
        {
        }

        public RowOrColumnMajor CurrentRowOrColumnMajor => Retry(obj => obj.CurrentRowOrColumnMajor);

        public RowOrColumnMajor CachedRowOrColumnMajor => Retry(obj => obj.CachedRowOrColumnMajor);

        public IUIAutomationElementArray GetCurrentRowHeaders() => Retry(obj => obj.GetCurrentRowHeaders());

        public IUIAutomationElementArray GetCurrentColumnHeaders() => Retry(obj => obj.GetCurrentColumnHeaders());

        public IUIAutomationElementArray GetCachedRowHeaders() => Retry(obj => obj.GetCachedRowHeaders());

        public IUIAutomationElementArray GetCachedColumnHeaders() => Retry(obj => obj.GetCachedColumnHeaders());
    }
}
