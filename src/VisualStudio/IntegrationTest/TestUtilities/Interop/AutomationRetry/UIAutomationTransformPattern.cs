// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationTransformPattern : AutomationRetryWrapper<IUIAutomationTransformPattern>, IUIAutomationTransformPattern
    {
        public UIAutomationTransformPattern(IUIAutomationTransformPattern automationObject)
            : base(automationObject)
        {
        }

        public int CurrentCanMove => Retry(obj => obj.CurrentCanMove);

        public int CurrentCanResize => Retry(obj => obj.CurrentCanResize);

        public int CurrentCanRotate => Retry(obj => obj.CurrentCanRotate);

        public int CachedCanMove => Retry(obj => obj.CachedCanMove);

        public int CachedCanResize => Retry(obj => obj.CachedCanResize);

        public int CachedCanRotate => Retry(obj => obj.CachedCanRotate);

        public void Move(double x, double y) => Retry(obj=> obj.Move(x,y));

        public void Resize(double width, double height) => Retry(obj => obj.Resize(width, height));

        public void Rotate(double degrees) => Retry(obj => obj.Rotate(degrees));
    }
}
