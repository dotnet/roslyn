// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationSpreadsheetItemPattern : AutomationRetryWrapper<IUIAutomationSpreadsheetItemPattern>, IUIAutomationSpreadsheetItemPattern
    {
        public UIAutomationSpreadsheetItemPattern(IUIAutomationSpreadsheetItemPattern automationObject)
            : base(automationObject)
        {
        }

        public string CurrentFormula => Retry(obj => obj.CurrentFormula);

        public string CachedFormula => Retry(obj => obj.CachedFormula);

        public IUIAutomationElementArray GetCurrentAnnotationObjects() => Retry(obj => obj.GetCurrentAnnotationObjects());

        public int[] GetCurrentAnnotationTypes() => Retry(obj => obj.GetCurrentAnnotationTypes());

        public IUIAutomationElementArray GetCachedAnnotationObjects() => Retry(obj => obj.GetCachedAnnotationObjects());

        public int[] GetCachedAnnotationTypes() => Retry(obj => obj.GetCachedAnnotationTypes());
    }
}
