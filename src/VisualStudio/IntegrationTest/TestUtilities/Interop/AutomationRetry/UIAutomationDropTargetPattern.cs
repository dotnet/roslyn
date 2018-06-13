// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationDropTargetPattern : AutomationRetryWrapper<IUIAutomationDropTargetPattern>, IUIAutomationDropTargetPattern
    {
        public UIAutomationDropTargetPattern(IUIAutomationDropTargetPattern automationObject)
            : base(automationObject)
        {
        }

        public string CurrentDropTargetEffect => Retry(obj => obj.CurrentDropTargetEffect);

        public string CachedDropTargetEffect => Retry(obj => obj.CachedDropTargetEffect);

        public string[] CurrentDropTargetEffects => Retry(obj => obj.CurrentDropTargetEffects);

        public string[] CachedDropTargetEffects => Retry(obj => obj.CachedDropTargetEffects);
    }
}
