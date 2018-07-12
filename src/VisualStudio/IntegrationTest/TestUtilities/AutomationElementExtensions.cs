// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UIAutomationClient;
using AutomationElementIdentifiers = System.Windows.Automation.AutomationElementIdentifiers;
using ControlType = System.Windows.Automation.ControlType;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class AutomationElementExtensions
    {
        private const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);

        /// <summary>
        /// The number of times to retry a UI automation operation that failed with
        /// <see cref="UIA_E_ELEMENTNOTAVAILABLE"/>, not counting the initial call. A value of 2 means the operation
        /// will be attempted a total of three times.
        /// </summary>
        private const int AutomationRetryCount = 2;

        /// <summary>
        /// The delay between retrying a UI automation operation that failed with
        /// <see cref="UIA_E_ELEMENTNOTAVAILABLE"/>.
        /// </summary>
        private static readonly TimeSpan AutomationRetryDelay = TimeSpan.FromMilliseconds(100);

        public static T GetCurrentPattern<T>(this IUIAutomationElement element, int patternId)
        {
            return RetryIfNotAvailable(
                e => (T)element.GetCurrentPattern(patternId),
                element);
        }

        /// <summary>
        /// Invokes an <see cref="IUIAutomationElement"/>.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="IUIAutomationInvokePattern"/>.
        /// </summary>
        public static void Invoke(this IUIAutomationElement element)
        {
            var invokePattern = element.GetCurrentPattern<IUIAutomationInvokePattern>(UIA_PatternIds.UIA_InvokePatternId);
            if (invokePattern != null)
            {
                RetryIfNotAvailable(
                    pattern => pattern.Invoke(),
                    invokePattern);
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the InvokePattern.");
            }
        }

        /// <summary>
        /// Given an <see cref="IUIAutomationElement"/>, returns a descendent following the <paramref name="path"/>.
        /// Throws an <see cref="InvalidOperationException"/> if no such descendant is found.
        /// </summary>

        public static IUIAutomationElement FindDescendantByPath(this IUIAutomationElement element, string path)
        {
            string[] pathParts = path.Split(".".ToCharArray());

            // traverse the path
            IUIAutomationElement item = element;
            IUIAutomationElement next = null;

            foreach (string pathPart in pathParts)
            {
                next = item.FindFirst(TreeScope.TreeScope_Descendants, Helper.Automation.CreatePropertyCondition(AutomationElementIdentifiers.LocalizedControlTypeProperty.Id, pathPart));
                
                if (next == null)
                {
                    ThrowUnableToFindChildException(path, item);
                }

                item = next;
            }

            return item;
        }

        private static void ThrowUnableToFindChildException(string path, IUIAutomationElement item)
        {
            // if not found, build a list of available children for debugging purposes
            var validChildren = new List<string>();

            try
            {
                var children = item.GetCachedChildren();
                for (int i = 0; i < children.Length; i++)
                {
                    validChildren.Add(SimpleControlTypeName(children.GetElement(i)));
                }
            }
            catch (InvalidOperationException)
            {
                // if the cached children can't be enumerated, don't blow up trying to display debug info
            }

            throw new InvalidOperationException(string.Format("Unable to find a child named {0}.  Possible values: ({1}).",
                path,
                string.Join(", ", validChildren)));
        }

        private static string SimpleControlTypeName(IUIAutomationElement element)
        {
            var type = ControlType.LookupById((int)element.GetCurrentPropertyValue(AutomationElementIdentifiers.ControlTypeProperty.Id));
            return type?.LocalizedControlType;
        }

        /// <summary>
        /// Given an <see cref="IUIAutomationElement"/> returns a string representing the "name" of the element, if it has one.
        /// </summary>
        private static string GetNameForExceptionMessage(this IUIAutomationElement element)
        {
            return RetryIfNotAvailable(e => e.CurrentAutomationId, element)
                ?? RetryIfNotAvailable(e => e.CurrentName, element)
                ?? "<unnamed>";
        }

        internal static void RetryIfNotAvailable<T>(Action<T> action, T state)
        {
            // NOTE: The loop termination condition if exceptions are thrown is in the exception filter
            for (var i = 0; true; i++)
            {
                try
                {
                    action(state);
                    return;
                }
                catch (COMException e) when (e.HResult == UIA_E_ELEMENTNOTAVAILABLE && i < AutomationRetryCount)
                {
                    Thread.Sleep(AutomationRetryDelay);
                    continue;
                }
            }
        }

        internal static TResult RetryIfNotAvailable<T, TResult>(Func<T, TResult> function, T state)
        {
            // NOTE: The loop termination condition if exceptions are thrown is in the exception filter
            for (var i = 0; true; i++)
            {
                try
                {
                    return function(state);
                }
                catch (COMException e) when (e.HResult == UIA_E_ELEMENTNOTAVAILABLE && i < AutomationRetryCount)
                {
                    Thread.Sleep(AutomationRetryDelay);
                    continue;
                }
            }
        }
    }
}
