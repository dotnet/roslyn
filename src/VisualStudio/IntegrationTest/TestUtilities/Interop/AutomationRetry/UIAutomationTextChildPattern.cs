// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationTextChildPattern : AutomationRetryWrapper<IUIAutomationTextChildPattern>, IUIAutomationTextChildPattern
    {
        public UIAutomationTextChildPattern(IUIAutomationTextChildPattern automationObject)
            : base(automationObject)
        {
        }

        public IUIAutomationElement TextContainer => Retry(obj => obj.TextContainer);

        public IUIAutomationTextRange TextRange => Retry(obj => obj.TextRange);
    }
}
