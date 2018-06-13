// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Roslyn.Utilities;
using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal static class AutomationRetryWrapper
    {
        private static readonly Dictionary<Type, Func<object, object>> _wrapperFunctions =
            new Dictionary<Type, Func<object, object>>
            {
                { typeof(IRawElementProviderSimple), obj => new RawElementProviderSimple((IRawElementProviderSimple)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomation), obj => new UIAutomation((IUIAutomation)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomation2), obj => new UIAutomation2((IUIAutomation2)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationAndCondition), obj => new UIAutomationAndCondition((IUIAutomationAndCondition)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationAnnotationPattern), obj => new UIAutomationAnnotationPattern((IUIAutomationAnnotationPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationBoolCondition), obj => new UIAutomationBoolCondition((IUIAutomationBoolCondition)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationCacheRequest), obj => new UIAutomationCacheRequest((IUIAutomationCacheRequest)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationCondition), obj => new UIAutomationCondition((IUIAutomationCondition)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationDockPattern), obj => new UIAutomationDockPattern((IUIAutomationDockPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationDragPattern), obj => new UIAutomationDragPattern((IUIAutomationDragPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationDropTargetPattern), obj => new UIAutomationDropTargetPattern((IUIAutomationDropTargetPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationElement), obj => new UIAutomationElement((IUIAutomationElement)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationElement2), obj => new UIAutomationElement2((IUIAutomationElement2)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationElementArray), obj => new UIAutomationElementArray((IUIAutomationElementArray)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationEventHandler), obj => new UIAutomationEventHandler((IUIAutomationEventHandler)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationExpandCollapsePattern), obj => new UIAutomationExpandCollapsePattern((IUIAutomationExpandCollapsePattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationFocusChangedEventHandler), obj => new UIAutomationFocusChangedEventHandler((IUIAutomationFocusChangedEventHandler)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationGridItemPattern), obj => new UIAutomationGridItemPattern((IUIAutomationGridItemPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationGridPattern), obj => new UIAutomationGridPattern((IUIAutomationGridPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationInvokePattern), obj => new UIAutomationInvokePattern((IUIAutomationInvokePattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationItemContainerPattern), obj => new UIAutomationItemContainerPattern((IUIAutomationItemContainerPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationLegacyIAccessiblePattern), obj => new UIAutomationLegacyIAccessiblePattern((IUIAutomationLegacyIAccessiblePattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationMultipleViewPattern), obj => new UIAutomationMultipleViewPattern((IUIAutomationMultipleViewPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationNotCondition), obj => new UIAutomationNotCondition((IUIAutomationNotCondition)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationObjectModelPattern), obj => new UIAutomationObjectModelPattern((IUIAutomationObjectModelPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationOrCondition), obj => new UIAutomationOrCondition((IUIAutomationOrCondition)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationPropertyChangedEventHandler), obj => new UIAutomationPropertyChangedEventHandler((IUIAutomationPropertyChangedEventHandler)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationPropertyCondition), obj => new UIAutomationPropertyCondition((IUIAutomationPropertyCondition)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationProxyFactory), obj => new UIAutomationProxyFactory((IUIAutomationProxyFactory)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationProxyFactoryEntry), obj => new UIAutomationProxyFactoryEntry((IUIAutomationProxyFactoryEntry)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationProxyFactoryMapping), obj => new UIAutomationProxyFactoryMapping((IUIAutomationProxyFactoryMapping)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationRangeValuePattern), obj => new UIAutomationRangeValuePattern((IUIAutomationRangeValuePattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationScrollItemPattern), obj => new UIAutomationScrollItemPattern((IUIAutomationScrollItemPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationScrollPattern), obj => new UIAutomationScrollPattern((IUIAutomationScrollPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationSelectionItemPattern), obj => new UIAutomationSelectionItemPattern((IUIAutomationSelectionItemPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationSelectionPattern), obj => new UIAutomationSelectionPattern((IUIAutomationSelectionPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationSpreadsheetItemPattern), obj => new UIAutomationSpreadsheetItemPattern((IUIAutomationSpreadsheetItemPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationSpreadsheetPattern), obj => new UIAutomationSpreadsheetPattern((IUIAutomationSpreadsheetPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationStructureChangedEventHandler), obj => new UIAutomationStructureChangedEventHandler((IUIAutomationStructureChangedEventHandler)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationStylesPattern), obj => new UIAutomationStylesPattern((IUIAutomationStylesPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationSynchronizedInputPattern), obj => new UIAutomationSynchronizedInputPattern((IUIAutomationSynchronizedInputPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationTableItemPattern), obj => new UIAutomationTableItemPattern((IUIAutomationTableItemPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationTablePattern), obj => new UIAutomationTablePattern((IUIAutomationTablePattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationTextChildPattern), obj => new UIAutomationTextChildPattern((IUIAutomationTextChildPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationTextPattern), obj => new UIAutomationTextPattern((IUIAutomationTextPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationTextPattern2), obj => new UIAutomationTextPattern2((IUIAutomationTextPattern2)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationTextRange), obj => new UIAutomationTextRange((IUIAutomationTextRange)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationTextRangeArray), obj => new UIAutomationTextRangeArray((IUIAutomationTextRangeArray)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationTogglePattern), obj => new UIAutomationTogglePattern((IUIAutomationTogglePattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationTransformPattern), obj => new UIAutomationTransformPattern((IUIAutomationTransformPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationTransformPattern2), obj => new UIAutomationTransformPattern2((IUIAutomationTransformPattern2)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationTreeWalker), obj => new UIAutomationTreeWalker((IUIAutomationTreeWalker)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationValuePattern), obj => new UIAutomationValuePattern((IUIAutomationValuePattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationVirtualizedItemPattern), obj => new UIAutomationVirtualizedItemPattern((IUIAutomationVirtualizedItemPattern)obj).RuntimeCallableWrapper },
                { typeof(IUIAutomationWindowPattern), obj => new UIAutomationWindowPattern((IUIAutomationWindowPattern)obj).RuntimeCallableWrapper },
            };
        private static readonly Dictionary<Guid, Func<IntPtr, IntPtr>> _nativeWrapperFunctions = new Dictionary<Guid, Func<IntPtr, IntPtr>>();

        static AutomationRetryWrapper()
        {
            foreach (var (type, wrapperFunction) in _wrapperFunctions)
            {
                _nativeWrapperFunctions.Add(type.GUID, unk => Marshal.GetIUnknownForObject(wrapperFunction(Marshal.GetObjectForIUnknown(unk))));
            }
        }

        public static T WrapIfNecessary<T>(T value)
        {
            if (value == null)
            {
                return default;
            }

            if (value is IRetryWrapper)
            {
                // This object is already wrapped
                return value;
            }

            if (_wrapperFunctions.TryGetValue(typeof(T), out var wrapperFunction))
            {
                return (T)wrapperFunction(value);
            }

            if (typeof(T) == typeof(object))
            {
                if (value is IUnknown unknown)
                {
                    return (T)new Unknown(unknown).RuntimeCallableWrapper;
                }
                else if (Marshal.IsComObject(value))
                {
                    var unk = Marshal.GetIUnknownForObject(value);
                    try
                    {
                        return (T)new Unknown((IUnknown)Marshal.GetObjectForIUnknown(unk)).RuntimeCallableWrapper;
                    }
                    finally
                    {
                        Marshal.Release(unk);
                    }
                }
            }

            // Objects which are not recognized automation objects are not wrapped
            return value;
        }

        public static IntPtr WrapNativeIfNecessary(in Guid guid, IntPtr unk)
        {
            if (unk == IntPtr.Zero)
            {
                return unk;
            }

            if (Marshal.GetObjectForIUnknown(unk) is IRetryWrapper)
            {
                // This object is already wrapped
                return unk;
            }

            if (_nativeWrapperFunctions.TryGetValue(guid, out var wrapperFunction))
            {
                return wrapperFunction(unk);
            }

            return unk;
        }

        public static T Unwrap<T>(T automationObject)
        {
            if (automationObject == null)
            {
                return default;
            }

            if (automationObject is IRetryWrapper retryWrapper)
            {
                return (T)retryWrapper.WrappedObject;
            }

            if (automationObject is object[] objArray)
            {
                var result = (object[])objArray.Clone();
                for (var i = 0; i < result.Length; i++)
                {
                    result[i] = Unwrap(result[i]);
                }

                return (T)(object)result;
            }

            return automationObject;
        }
    }
}
