// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomation2 : AutomationRetryWrapper<IUIAutomation2>, IUIAutomation2
    {
        public UIAutomation2(IUIAutomation2 automationObject)
            : base(automationObject)
        {
        }

        public IUIAutomationTreeWalker ControlViewWalker => Retry(obj => obj.ControlViewWalker);

        public IUIAutomationTreeWalker ContentViewWalker => Retry(obj => obj.ContentViewWalker);

        public IUIAutomationTreeWalker RawViewWalker => Retry(obj => obj.RawViewWalker);

        public IUIAutomationCondition RawViewCondition => Retry(obj => obj.RawViewCondition);

        public IUIAutomationCondition ControlViewCondition => Retry(obj => obj.ControlViewCondition);

        public IUIAutomationCondition ContentViewCondition => Retry(obj => obj.ContentViewCondition);

        public IUIAutomationProxyFactoryMapping ProxyFactoryMapping => Retry(obj => obj.ProxyFactoryMapping);

        public object ReservedNotSupportedValue => Retry(obj => obj.ReservedNotSupportedValue);

        public object ReservedMixedAttributeValue => Retry(obj => obj.ReservedMixedAttributeValue);

        public int AutoSetFocus
        {
            get => Retry(obj => obj.AutoSetFocus);
            set => Retry(obj => obj.AutoSetFocus = value);
        }

        public uint ConnectionTimeout
        {
            get => Retry(obj => obj.ConnectionTimeout);
            set => Retry(obj => obj.ConnectionTimeout = value);
        }

        public uint TransactionTimeout
        {
            get => Retry(obj => obj.TransactionTimeout);
            set => Retry(obj => obj.TransactionTimeout = value);
        }

        public int CompareElements(IUIAutomationElement el1, IUIAutomationElement el2) => Retry(obj => obj.CompareElements(AutomationRetryWrapper.Unwrap(el1), AutomationRetryWrapper.Unwrap(el2)));

        public int CompareRuntimeIds(int[] runtimeId1, int[] runtimeId2) => Retry(obj => obj.CompareRuntimeIds(runtimeId1, runtimeId2));

        public IUIAutomationElement GetRootElement() => Retry(obj => obj.GetRootElement());

        public IUIAutomationElement ElementFromHandle(IntPtr hwnd) => Retry(obj => obj.ElementFromHandle(hwnd));

        public IUIAutomationElement ElementFromPoint(tagPOINT pt) => Retry(obj => obj.ElementFromPoint(pt));

        public IUIAutomationElement GetFocusedElement() => Retry(obj => obj.GetFocusedElement());

        public IUIAutomationElement GetRootElementBuildCache(IUIAutomationCacheRequest cacheRequest) => Retry(obj => obj.GetRootElementBuildCache(AutomationRetryWrapper.Unwrap(cacheRequest)));

        public IUIAutomationElement ElementFromHandleBuildCache(IntPtr hwnd, IUIAutomationCacheRequest cacheRequest) => Retry(obj => obj.ElementFromHandleBuildCache(hwnd, AutomationRetryWrapper.Unwrap(cacheRequest)));

        public IUIAutomationElement ElementFromPointBuildCache(tagPOINT pt, IUIAutomationCacheRequest cacheRequest) => Retry(obj => obj.ElementFromPointBuildCache(pt, AutomationRetryWrapper.Unwrap(cacheRequest)));

        public IUIAutomationElement GetFocusedElementBuildCache(IUIAutomationCacheRequest cacheRequest) => Retry(obj => obj.GetFocusedElementBuildCache(AutomationRetryWrapper.Unwrap(cacheRequest)));

        public IUIAutomationTreeWalker CreateTreeWalker(IUIAutomationCondition pCondition) => Retry(obj => obj.CreateTreeWalker(AutomationRetryWrapper.Unwrap(pCondition)));

        public IUIAutomationCacheRequest CreateCacheRequest() => Retry(obj => obj.CreateCacheRequest());

        public IUIAutomationCondition CreateTrueCondition() => Retry(obj => obj.CreateTrueCondition());

        public IUIAutomationCondition CreateFalseCondition() => Retry(obj => obj.CreateFalseCondition());

        public IUIAutomationCondition CreatePropertyCondition(int propertyId, object value) => Retry(obj => obj.CreatePropertyCondition(propertyId, AutomationRetryWrapper.Unwrap(value)));

        public IUIAutomationCondition CreatePropertyConditionEx(int propertyId, object value, PropertyConditionFlags flags) => Retry(obj => obj.CreatePropertyConditionEx(propertyId, AutomationRetryWrapper.Unwrap(value), flags));

        public IUIAutomationCondition CreateAndCondition(IUIAutomationCondition condition1, IUIAutomationCondition condition2) => Retry(obj => obj.CreateAndCondition(AutomationRetryWrapper.Unwrap(condition1), AutomationRetryWrapper.Unwrap(condition2)));

        public IUIAutomationCondition CreateAndConditionFromArray(IUIAutomationCondition[] conditions) => Retry(obj => obj.CreateAndConditionFromArray(AutomationRetryWrapper.Unwrap(conditions)));

        public IUIAutomationCondition CreateAndConditionFromNativeArray(ref IUIAutomationCondition conditions, int conditionCount) => AutomationObject.CreateAndConditionFromNativeArray(conditions, conditionCount);

        public IUIAutomationCondition CreateOrCondition(IUIAutomationCondition condition1, IUIAutomationCondition condition2) => Retry(obj => obj.CreateOrCondition(AutomationRetryWrapper.Unwrap(condition1), AutomationRetryWrapper.Unwrap(condition2)));

        public IUIAutomationCondition CreateOrConditionFromArray(IUIAutomationCondition[] conditions) => Retry(obj => obj.CreateOrConditionFromArray(AutomationRetryWrapper.Unwrap(conditions)));

        public IUIAutomationCondition CreateOrConditionFromNativeArray(ref IUIAutomationCondition conditions, int conditionCount) => AutomationObject.CreateOrConditionFromNativeArray(conditions, conditionCount);

        public IUIAutomationCondition CreateNotCondition(IUIAutomationCondition condition) => Retry(obj => obj.CreateNotCondition(AutomationRetryWrapper.Unwrap(condition)));

        public void AddAutomationEventHandler(int eventId, IUIAutomationElement element, TreeScope scope, IUIAutomationCacheRequest cacheRequest, IUIAutomationEventHandler handler)
            => Retry(obj => obj.AddAutomationEventHandler(eventId, AutomationRetryWrapper.Unwrap(element), scope, AutomationRetryWrapper.Unwrap(cacheRequest), AutomationRetryWrapper.Unwrap(handler)));

        public void RemoveAutomationEventHandler(int eventId, IUIAutomationElement element, IUIAutomationEventHandler handler)
            => Retry(obj => obj.RemoveAutomationEventHandler(eventId, AutomationRetryWrapper.Unwrap(element), AutomationRetryWrapper.Unwrap(handler)));

        public void AddPropertyChangedEventHandlerNativeArray(IUIAutomationElement element, TreeScope scope, IUIAutomationCacheRequest cacheRequest, IUIAutomationPropertyChangedEventHandler handler, ref int propertyArray, int propertyCount)
            => AutomationObject.AddPropertyChangedEventHandlerNativeArray(AutomationRetryWrapper.Unwrap(element), scope, AutomationRetryWrapper.Unwrap(cacheRequest), AutomationRetryWrapper.Unwrap(handler), propertyArray, propertyCount);

        public void AddPropertyChangedEventHandler(IUIAutomationElement element, TreeScope scope, IUIAutomationCacheRequest cacheRequest, IUIAutomationPropertyChangedEventHandler handler, int[] propertyArray)
            => Retry(obj => obj.AddPropertyChangedEventHandler(AutomationRetryWrapper.Unwrap(element), scope, AutomationRetryWrapper.Unwrap(cacheRequest), AutomationRetryWrapper.Unwrap(handler), propertyArray));

        public void RemovePropertyChangedEventHandler(IUIAutomationElement element, IUIAutomationPropertyChangedEventHandler handler)
            => Retry(obj => obj.RemovePropertyChangedEventHandler(AutomationRetryWrapper.Unwrap(element), AutomationRetryWrapper.Unwrap(handler)));

        public void AddStructureChangedEventHandler(IUIAutomationElement element, TreeScope scope, IUIAutomationCacheRequest cacheRequest, IUIAutomationStructureChangedEventHandler handler)
            => Retry(obj => obj.AddStructureChangedEventHandler(AutomationRetryWrapper.Unwrap(element), scope, AutomationRetryWrapper.Unwrap(cacheRequest), AutomationRetryWrapper.Unwrap(handler)));

        public void RemoveStructureChangedEventHandler(IUIAutomationElement element, IUIAutomationStructureChangedEventHandler handler)
            => Retry(obj => obj.RemoveStructureChangedEventHandler(AutomationRetryWrapper.Unwrap(element), AutomationRetryWrapper.Unwrap(handler)));

        public void AddFocusChangedEventHandler(IUIAutomationCacheRequest cacheRequest, IUIAutomationFocusChangedEventHandler handler)
            => Retry(obj => obj.AddFocusChangedEventHandler(AutomationRetryWrapper.Unwrap(cacheRequest), AutomationRetryWrapper.Unwrap(handler)));

        public void RemoveFocusChangedEventHandler(IUIAutomationFocusChangedEventHandler handler)
            => Retry(obj => obj.RemoveFocusChangedEventHandler(AutomationRetryWrapper.Unwrap(handler)));

        public void RemoveAllEventHandlers() => Retry(obj => obj.RemoveAllEventHandlers());

        public int[] IntNativeArrayToSafeArray(ref int array, int arrayCount) => AutomationObject.IntNativeArrayToSafeArray(array, arrayCount);

        public int IntSafeArrayToNativeArray(int[] intArray, IntPtr array) => Retry(obj => obj.IntSafeArrayToNativeArray(intArray, array));

        public object RectToVariant(tagRECT rc) => Retry(obj => obj.RectToVariant(rc));

        public tagRECT VariantToRect(object var) => Retry(obj => obj.VariantToRect(AutomationRetryWrapper.Unwrap(var)));

        public int SafeArrayToRectNativeArray(double[] rects, IntPtr rectArray) => Retry(obj => obj.SafeArrayToRectNativeArray(rects, rectArray));

        public IUIAutomationProxyFactoryEntry CreateProxyFactoryEntry(IUIAutomationProxyFactory factory) => Retry(obj => obj.CreateProxyFactoryEntry(factory));

        public string GetPropertyProgrammaticName(int property) => Retry(obj => obj.GetPropertyProgrammaticName(property));

        public string GetPatternProgrammaticName(int pattern) => Retry(obj => obj.GetPatternProgrammaticName(pattern));

        public void PollForPotentialSupportedPatterns(IUIAutomationElement pElement, out int[] patternIds, out string[] patternNames)
        {
            int[] patternIdsResult = null;
            string[] patternNamesResult = null;
            Retry(obj => obj.PollForPotentialSupportedPatterns(pElement, out patternIdsResult, out patternNamesResult));
            patternIds = patternIdsResult;
            patternNames = patternNamesResult;
        }

        public void PollForPotentialSupportedProperties(IUIAutomationElement pElement, out int[] propertyIds, out string[] propertyNames)
        {
            int[] propertyIdsResult = null;
            string[] propertyNamesResult = null;
            Retry(obj => obj.PollForPotentialSupportedProperties(pElement, out propertyIdsResult, out propertyNamesResult));
            propertyIds = propertyIdsResult;
            propertyNames = propertyNamesResult;
        }

        public int CheckNotSupported(object value) => Retry(obj => obj.CheckNotSupported(AutomationRetryWrapper.Unwrap(value)));

        public IUIAutomationElement ElementFromIAccessible(IAccessible accessible, int childId)
            => Retry(obj => obj.ElementFromIAccessible(AutomationRetryWrapper.Unwrap(accessible), childId));

        public IUIAutomationElement ElementFromIAccessibleBuildCache(IAccessible accessible, int childId, IUIAutomationCacheRequest cacheRequest)
            => Retry(obj => obj.ElementFromIAccessibleBuildCache(AutomationRetryWrapper.Unwrap(accessible), childId, AutomationRetryWrapper.Unwrap(cacheRequest)));
    }
}
