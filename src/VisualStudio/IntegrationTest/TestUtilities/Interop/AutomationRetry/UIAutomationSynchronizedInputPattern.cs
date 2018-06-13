// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationSynchronizedInputPattern : AutomationRetryWrapper<IUIAutomationSynchronizedInputPattern>, IUIAutomationSynchronizedInputPattern
    {
        public UIAutomationSynchronizedInputPattern(IUIAutomationSynchronizedInputPattern automationObject)
            : base(automationObject)
        {
        }

        public void StartListening(SynchronizedInputType inputType) => Retry(obj => obj.StartListening(inputType));

        public void Cancel() => Retry(obj => obj.Cancel());
    }
}
