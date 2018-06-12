// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationTransformPattern2 : AutomationRetryWrapper<IUIAutomationTransformPattern2>, IUIAutomationTransformPattern2
    {
        public UIAutomationTransformPattern2(IUIAutomationTransformPattern2 automationObject)
            : base(automationObject)
        {
        }

        public int CurrentCanMove => Retry(obj => obj.CurrentCanMove);

        public int CurrentCanResize => Retry(obj => obj.CurrentCanResize);

        public int CurrentCanRotate => Retry(obj => obj.CurrentCanRotate);

        public int CachedCanMove => Retry(obj => obj.CachedCanMove);

        public int CachedCanResize => Retry(obj => obj.CachedCanResize);

        public int CachedCanRotate => Retry(obj => obj.CachedCanRotate);

        public int CurrentCanZoom => Retry(obj => obj.CurrentCanZoom);

        public int CachedCanZoom => Retry(obj => obj.CachedCanZoom);

        public double CurrentZoomLevel => Retry(obj => obj.CurrentZoomLevel);

        public double CachedZoomLevel => Retry(obj => obj.CachedZoomLevel);

        public double CurrentZoomMinimum => Retry(obj => obj.CurrentZoomMinimum);

        public double CachedZoomMinimum => Retry(obj => obj.CachedZoomMinimum);

        public double CurrentZoomMaximum => Retry(obj => obj.CurrentZoomMaximum);

        public double CachedZoomMaximum => Retry(obj => obj.CachedZoomMaximum);

        public void Move(double x, double y) => Retry(obj => obj.Move(x, y));

        public void Resize(double width, double height) => Retry(obj => obj.Resize(width, height));

        public void Rotate(double degrees) => Retry(obj => obj.Rotate(degrees));

        public void Zoom(double Zoom) => Retry(obj => obj.Zoom(Zoom));

        public void ZoomByUnit(ZoomUnit ZoomUnit) => Retry(obj => obj.ZoomByUnit(ZoomUnit));
    }
}
