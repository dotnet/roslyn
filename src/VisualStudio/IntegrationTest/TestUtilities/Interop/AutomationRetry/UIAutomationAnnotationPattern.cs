// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationAnnotationPattern : AutomationRetryWrapper<IUIAutomationAnnotationPattern>, IUIAutomationAnnotationPattern
    {
        public UIAutomationAnnotationPattern(IUIAutomationAnnotationPattern automationObject)
            : base(automationObject)
        {
        }

        public int CurrentAnnotationTypeId => Retry(obj => obj.CurrentAnnotationTypeId);

        public string CurrentAnnotationTypeName => Retry(obj => obj.CurrentAnnotationTypeName);

        public string CurrentAuthor => Retry(obj => obj.CurrentAuthor);

        public string CurrentDateTime => Retry(obj => obj.CurrentDateTime);

        public IUIAutomationElement CurrentTarget => Retry(obj => obj.CurrentTarget);

        public int CachedAnnotationTypeId => Retry(obj => obj.CachedAnnotationTypeId);

        public string CachedAnnotationTypeName => Retry(obj => obj.CachedAnnotationTypeName);

        public string CachedAuthor => Retry(obj => obj.CachedAuthor);

        public string CachedDateTime => Retry(obj => obj.CachedDateTime);

        public IUIAutomationElement CachedTarget => Retry(obj => obj.CachedTarget);
    }
}
