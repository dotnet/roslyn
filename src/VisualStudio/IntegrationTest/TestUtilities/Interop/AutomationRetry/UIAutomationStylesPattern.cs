// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationStylesPattern : AutomationRetryWrapper<IUIAutomationStylesPattern>, IUIAutomationStylesPattern
    {
        public UIAutomationStylesPattern(IUIAutomationStylesPattern automationObject)
            : base(automationObject)
        {
        }

        public int CurrentStyleId => Retry(obj => obj.CurrentStyleId);

        public string CurrentStyleName => Retry(obj => obj.CurrentStyleName);

        public int CurrentFillColor => Retry(obj => obj.CurrentFillColor);

        public string CurrentFillPatternStyle => Retry(obj => obj.CurrentFillPatternStyle);

        public string CurrentShape => Retry(obj => obj.CurrentShape);

        public int CurrentFillPatternColor => Retry(obj => obj.CurrentFillPatternColor);

        public string CurrentExtendedProperties => Retry(obj => obj.CurrentExtendedProperties);

        public int CachedStyleId => Retry(obj => obj.CachedStyleId);

        public string CachedStyleName => Retry(obj => obj.CachedStyleName);

        public int CachedFillColor => Retry(obj => obj.CachedFillColor);

        public string CachedFillPatternStyle => Retry(obj => obj.CachedFillPatternStyle);

        public string CachedShape => Retry(obj => obj.CachedShape);

        public int CachedFillPatternColor => Retry(obj => obj.CachedFillPatternColor);

        public string CachedExtendedProperties => Retry(obj => obj.CachedExtendedProperties);

        public void GetCurrentExtendedPropertiesAsArray(IntPtr propertyArray, out int propertyCount)
        {
            var propertyCountResult = 0;
            Retry(obj => obj.GetCurrentExtendedPropertiesAsArray(propertyArray, out propertyCountResult));
            propertyCount = propertyCountResult;
        }

        public void GetCachedExtendedPropertiesAsArray(IntPtr propertyArray, out int propertyCount)
        {
            var propertyCountResult = 0;
            Retry(obj => obj.GetCachedExtendedPropertiesAsArray(propertyArray, out propertyCountResult));
            propertyCount = propertyCountResult;
        }
    }
}
