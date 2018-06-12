// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationDockPattern : AutomationRetryWrapper<IUIAutomationDockPattern>, IUIAutomationDockPattern
    {
        public UIAutomationDockPattern(IUIAutomationDockPattern automationObject)
            : base(automationObject)
        {
        }

        public DockPosition CurrentDockPosition => Retry(obj => obj.CurrentDockPosition);

        public DockPosition CachedDockPosition => Retry(obj => obj.CachedDockPosition);

        public void SetDockPosition(DockPosition dockPos) => Retry(obj => obj.SetDockPosition(dockPos));
    }
}
