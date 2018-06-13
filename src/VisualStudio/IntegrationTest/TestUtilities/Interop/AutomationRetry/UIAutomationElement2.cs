// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationElement2 : AutomationRetryWrapper<IUIAutomationElement2>, IUIAutomationElement2
    {
        public UIAutomationElement2(IUIAutomationElement2 automationObject)
            : base(automationObject)
        {
        }

        public int CurrentProcessId => Retry(obj => obj.CurrentProcessId);

        public int CurrentControlType => Retry(obj => obj.CurrentControlType);

        public string CurrentLocalizedControlType => Retry(obj => obj.CurrentLocalizedControlType);

        public string CurrentName => Retry(obj => obj.CurrentName);

        public string CurrentAcceleratorKey => Retry(obj => obj.CurrentAcceleratorKey);

        public string CurrentAccessKey => Retry(obj => obj.CurrentAccessKey);

        public int CurrentHasKeyboardFocus => Retry(obj => obj.CurrentHasKeyboardFocus);

        public int CurrentIsKeyboardFocusable => Retry(obj => obj.CurrentIsKeyboardFocusable);

        public int CurrentIsEnabled => Retry(obj => obj.CurrentIsEnabled);

        public string CurrentAutomationId => Retry(obj => obj.CurrentAutomationId);

        public string CurrentClassName => Retry(obj => obj.CurrentClassName);

        public string CurrentHelpText => Retry(obj => obj.CurrentHelpText);

        public int CurrentCulture => Retry(obj => obj.CurrentCulture);

        public int CurrentIsControlElement => Retry(obj => obj.CurrentIsControlElement);

        public int CurrentIsContentElement => Retry(obj => obj.CurrentIsContentElement);

        public int CurrentIsPassword => Retry(obj => obj.CurrentIsPassword);

        public IntPtr CurrentNativeWindowHandle => Retry(obj => obj.CurrentNativeWindowHandle);

        public string CurrentItemType => Retry(obj => obj.CurrentItemType);

        public int CurrentIsOffscreen => Retry(obj => obj.CurrentIsOffscreen);

        public OrientationType CurrentOrientation => Retry(obj => obj.CurrentOrientation);

        public string CurrentFrameworkId => Retry(obj => obj.CurrentFrameworkId);

        public int CurrentIsRequiredForForm => Retry(obj => obj.CurrentIsRequiredForForm);

        public string CurrentItemStatus => Retry(obj => obj.CurrentItemStatus);

        public tagRECT CurrentBoundingRectangle => Retry(obj => obj.CurrentBoundingRectangle);

        public IUIAutomationElement CurrentLabeledBy => Retry(obj => obj.CurrentLabeledBy);

        public string CurrentAriaRole => Retry(obj => obj.CurrentAriaRole);

        public string CurrentAriaProperties => Retry(obj => obj.CurrentAriaProperties);

        public int CurrentIsDataValidForForm => Retry(obj => obj.CurrentIsDataValidForForm);

        public IUIAutomationElementArray CurrentControllerFor => Retry(obj => obj.CurrentControllerFor);

        public IUIAutomationElementArray CurrentDescribedBy => Retry(obj => obj.CurrentDescribedBy);

        public IUIAutomationElementArray CurrentFlowsTo => Retry(obj => obj.CurrentFlowsTo);

        public string CurrentProviderDescription => Retry(obj => obj.CurrentProviderDescription);

        public int CachedProcessId => Retry(obj => obj.CachedProcessId);

        public int CachedControlType => Retry(obj => obj.CachedControlType);

        public string CachedLocalizedControlType => Retry(obj => obj.CachedLocalizedControlType);

        public string CachedName => Retry(obj => obj.CachedName);

        public string CachedAcceleratorKey => Retry(obj => obj.CachedAcceleratorKey);

        public string CachedAccessKey => Retry(obj => obj.CachedAccessKey);

        public int CachedHasKeyboardFocus => Retry(obj => obj.CachedHasKeyboardFocus);

        public int CachedIsKeyboardFocusable => Retry(obj => obj.CachedIsKeyboardFocusable);

        public int CachedIsEnabled => Retry(obj => obj.CachedIsEnabled);

        public string CachedAutomationId => Retry(obj => obj.CachedAutomationId);

        public string CachedClassName => Retry(obj => obj.CachedClassName);

        public string CachedHelpText => Retry(obj => obj.CachedHelpText);

        public int CachedCulture => Retry(obj => obj.CachedCulture);

        public int CachedIsControlElement => Retry(obj => obj.CachedIsControlElement);

        public int CachedIsContentElement => Retry(obj => obj.CachedIsContentElement);

        public int CachedIsPassword => Retry(obj => obj.CachedIsPassword);

        public IntPtr CachedNativeWindowHandle => Retry(obj => obj.CachedNativeWindowHandle);

        public string CachedItemType => Retry(obj => obj.CachedItemType);

        public int CachedIsOffscreen => Retry(obj => obj.CachedIsOffscreen);

        public OrientationType CachedOrientation => Retry(obj => obj.CachedOrientation);

        public string CachedFrameworkId => Retry(obj => obj.CachedFrameworkId);

        public int CachedIsRequiredForForm => Retry(obj => obj.CachedIsRequiredForForm);

        public string CachedItemStatus => Retry(obj => obj.CachedItemStatus);

        public tagRECT CachedBoundingRectangle => Retry(obj => obj.CachedBoundingRectangle);

        public IUIAutomationElement CachedLabeledBy => Retry(obj => obj.CachedLabeledBy);

        public string CachedAriaRole => Retry(obj => obj.CachedAriaRole);

        public string CachedAriaProperties => Retry(obj => obj.CachedAriaProperties);

        public int CachedIsDataValidForForm => Retry(obj => obj.CachedIsDataValidForForm);

        public IUIAutomationElementArray CachedControllerFor => Retry(obj => obj.CachedControllerFor);

        public IUIAutomationElementArray CachedDescribedBy => Retry(obj => obj.CachedDescribedBy);

        public IUIAutomationElementArray CachedFlowsTo => Retry(obj => obj.CachedFlowsTo);

        public string CachedProviderDescription => Retry(obj => obj.CachedProviderDescription);

        public int CurrentOptimizeForVisualContent => Retry(obj => obj.CurrentOptimizeForVisualContent);

        public int CachedOptimizeForVisualContent => Retry(obj => obj.CachedOptimizeForVisualContent);

        public LiveSetting CurrentLiveSetting => Retry(obj => obj.CurrentLiveSetting);

        public LiveSetting CachedLiveSetting => Retry(obj => obj.CachedLiveSetting);

        public IUIAutomationElementArray CurrentFlowsFrom => Retry(obj => obj.CurrentFlowsFrom);

        public IUIAutomationElementArray CachedFlowsFrom => Retry(obj => obj.CachedFlowsFrom);

        public void SetFocus() => Retry(obj => obj.SetFocus());

        public int[] GetRuntimeId() => Retry(obj => obj.GetRuntimeId());

        public IUIAutomationElement FindFirst(TreeScope scope, IUIAutomationCondition condition)
            => Retry(obj => obj.FindFirst(scope, AutomationRetryWrapper.Unwrap(condition)));

        public IUIAutomationElementArray FindAll(TreeScope scope, IUIAutomationCondition condition)
            => Retry(obj => obj.FindAll(scope, AutomationRetryWrapper.Unwrap(condition)));

        public IUIAutomationElement FindFirstBuildCache(TreeScope scope, IUIAutomationCondition condition, IUIAutomationCacheRequest cacheRequest)
            => Retry(obj => obj.FindFirstBuildCache(scope, AutomationRetryWrapper.Unwrap(condition), AutomationRetryWrapper.Unwrap(cacheRequest)));

        public IUIAutomationElementArray FindAllBuildCache(TreeScope scope, IUIAutomationCondition condition, IUIAutomationCacheRequest cacheRequest)
            => Retry(obj => obj.FindAllBuildCache(scope, AutomationRetryWrapper.Unwrap(condition), AutomationRetryWrapper.Unwrap(cacheRequest)));

        public IUIAutomationElement BuildUpdatedCache(IUIAutomationCacheRequest cacheRequest)
            => Retry(obj => obj.BuildUpdatedCache(AutomationRetryWrapper.Unwrap(cacheRequest)));

        public object GetCurrentPropertyValue(int propertyId) => Retry(obj => obj.GetCurrentPropertyValue(propertyId));

        public object GetCurrentPropertyValueEx(int propertyId, int ignoreDefaultValue) => Retry(obj => obj.GetCurrentPropertyValueEx(propertyId, ignoreDefaultValue));

        public object GetCachedPropertyValue(int propertyId) => Retry(obj => obj.GetCachedPropertyValue(propertyId));

        public object GetCachedPropertyValueEx(int propertyId, int ignoreDefaultValue) => Retry(obj => obj.GetCachedPropertyValueEx(propertyId, ignoreDefaultValue));

        public IntPtr GetCurrentPatternAs(int patternId, ref Guid riid)
        {
            var riidResult = riid;
            var result = Retry(obj => obj.GetCurrentPatternAs(patternId, ref riidResult));
            riid = riidResult;
            return result;
        }

        public IntPtr GetCachedPatternAs(int patternId, ref Guid riid)
        {
            var riidResult = riid;
            var result = Retry(obj => obj.GetCachedPatternAs(patternId, ref riidResult));
            riid = riidResult;
            return result;
        }

        public object GetCurrentPattern(int patternId) => Retry(obj => obj.GetCurrentPattern(patternId));

        public object GetCachedPattern(int patternId) => Retry(obj => obj.GetCachedPattern(patternId));

        public IUIAutomationElement GetCachedParent() => Retry(obj => obj.GetCachedParent());

        public IUIAutomationElementArray GetCachedChildren() => Retry(obj => obj.GetCachedChildren());

        public int GetClickablePoint(out tagPOINT clickable)
        {
            tagPOINT clickableResult = default;
            var result = Retry(obj => obj.GetClickablePoint(out clickableResult));
            clickable = clickableResult;
            return result;
        }
    }
}
