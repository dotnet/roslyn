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

        /// <summary>
        /// Given an <see cref="IUIAutomationElement"/>, returns a descendant with the automation ID specified by <paramref name="automationId"/>.
        /// Throws an <see cref="InvalidOperationException"/> if no such descendant is found.
        /// </summary>
        public static IUIAutomationElement FindDescendantByAutomationId(this IUIAutomationElement parent, string automationId)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var condition = Helper.Automation.CreatePropertyCondition(AutomationElementIdentifiers.AutomationIdProperty.Id, automationId);
            var child = Helper.Retry(
                () => parent.FindFirst(TreeScope.TreeScope_Descendants, condition),
                AutomationRetryDelay,
                retryCount: AutomationRetryCount);

            if (child == null)
            {
                throw new InvalidOperationException($"Could not find item with Automation ID '{automationId}' under '{parent.GetNameForExceptionMessage()}'.");
            }

            return child;
        }

        /// <summary>
        /// Given an <see cref="IUIAutomationElement"/>, returns a descendant with the automation ID specified by <paramref name="name"/>.
        /// Throws an <see cref="InvalidOperationException"/> if no such descendant is found.
        /// </summary>
        public static IUIAutomationElement FindDescendantByName(this IUIAutomationElement parent, string name)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var condition = Helper.Automation.CreatePropertyCondition(AutomationElementIdentifiers.NameProperty.Id, name);
            var child = Helper.Retry(
                () => parent.FindFirst(TreeScope.TreeScope_Descendants, condition),
                AutomationRetryDelay,
                retryCount: AutomationRetryCount);

            if (child == null)
            {
                throw new InvalidOperationException($"Could not find item with name '{name}' under '{parent.GetNameForExceptionMessage()}'.");
            }

            return child;
        }

        /// <summary>
        /// Given an <see cref="IUIAutomationElement"/>, returns a descendant with the className specified by <paramref name="className"/>.
        /// Throws an <see cref="InvalidOperationException"/> if no such descendant is found.
        /// </summary>
        public static IUIAutomationElement FindDescendantByClass(this IUIAutomationElement parent, string className)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var condition = Helper.Automation.CreatePropertyCondition(AutomationElementIdentifiers.ClassNameProperty.Id, className);
            var child = Helper.Retry(
                () => parent.FindFirst(TreeScope.TreeScope_Descendants, condition),
                AutomationRetryDelay,
                retryCount: AutomationRetryCount);

            if (child == null)
            {
                throw new InvalidOperationException($"Could not find item with class '{className}' under '{parent.GetNameForExceptionMessage()}'.");
            }

            return child;
        }

        /// <summary>
        /// Given an <see cref="IUIAutomationElement"/>, returns all descendants with the given <paramref name="className"/>.
        /// If none are found, the resulting collection will be empty.
        /// </summary>
        /// <returns></returns>
        public static IUIAutomationElementArray FindDescendantsByClass(this IUIAutomationElement parent, string className)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var condition = Helper.Automation.CreatePropertyCondition(AutomationElementIdentifiers.ClassNameProperty.Id, className);
            return parent.FindAll(TreeScope.TreeScope_Descendants, condition);
        }

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
        /// Expands an <see cref="IUIAutomationElement"/>.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="IUIAutomationExpandCollapsePattern"/>.
        /// </summary>
        public static void Expand(this IUIAutomationElement element)
        {
            var expandCollapsePattern = element.GetCurrentPattern<IUIAutomationExpandCollapsePattern>(UIA_PatternIds.UIA_ExpandCollapsePatternId);
            if (expandCollapsePattern != null)
            {
                RetryIfNotAvailable(
                    pattern => pattern.Expand(),
                    expandCollapsePattern);
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the ExpandCollapsePattern.");
            }
        }

        /// <summary>
        /// Collapses an <see cref="IUIAutomationElement"/>.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="IUIAutomationExpandCollapsePattern"/>.
        /// </summary>
        public static void Collapse(this IUIAutomationElement element)
        {
            var expandCollapsePattern = element.GetCurrentPattern<IUIAutomationExpandCollapsePattern>(UIA_PatternIds.UIA_ExpandCollapsePatternId);
            if (expandCollapsePattern != null)
            {
                RetryIfNotAvailable(
                    pattern => pattern.Collapse(),
                    expandCollapsePattern);
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the ExpandCollapsePattern.");
            }
        }

        /// <summary>
        /// Selects an <see cref="IUIAutomationElement"/>.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="IUIAutomationSelectionItemPattern"/>.
        /// </summary>
        public static void Select(this IUIAutomationElement element)
        {
            var selectionItemPattern = element.GetCurrentPattern<IUIAutomationSelectionItemPattern>(UIA_PatternIds.UIA_SelectionItemPatternId);
            if (selectionItemPattern != null)
            {
                RetryIfNotAvailable(
                    pattern => pattern.Select(),
                    selectionItemPattern);
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the SelectionItemPattern.");
            }
        }

        /// <summary>
        /// Gets the value of the given <see cref="IUIAutomationElement"/>.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="IUIAutomationValuePattern"/>.
        /// </summary>
        public static string GetValue(this IUIAutomationElement element)
        {
            var valuePattern = element.GetCurrentPattern<IUIAutomationValuePattern>(UIA_PatternIds.UIA_ValuePatternId);
            if (valuePattern != null)
            {
                return RetryIfNotAvailable(
                    pattern => pattern.CurrentValue,
                    valuePattern);
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the ValuePattern.");
            }
        }

        /// <summary>
        /// Sets the value of the given <see cref="IUIAutomationElement"/>.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="IUIAutomationValuePattern"/>.
        /// </summary>
        public static void SetValue(this IUIAutomationElement element, string value)
        {
            var valuePattern = element.GetCurrentPattern<IUIAutomationValuePattern>(UIA_PatternIds.UIA_ValuePatternId);
            if (valuePattern != null)
            {
                RetryIfNotAvailable(
                    pattern => pattern.SetValue(value),
                    valuePattern);
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the ValuePattern.");
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
        /// Returns true if the given <see cref="IUIAutomationElement"/> is in the <see cref="ToggleState.ToggleState_On"/> state.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="IUIAutomationTogglePattern"/>.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static bool IsToggledOn(this IUIAutomationElement element)
        {
            var togglePattern = element.GetCurrentPattern<IUIAutomationTogglePattern>(UIA_PatternIds.UIA_TogglePatternId);
            if (togglePattern != null)
            {
                return RetryIfNotAvailable(
                    pattern => pattern.CurrentToggleState == ToggleState.ToggleState_On,
                    togglePattern);
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the TogglePattern.");
            }
        }

        /// <summary>
        /// Cycles through the <see cref="ToggleState"/>s of the given <see cref="IUIAutomationElement"/>.
        /// </summary>
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="IUIAutomationTogglePattern"/>.
        public static void Toggle(this IUIAutomationElement element)
        {
            var togglePattern = element.GetCurrentPattern<IUIAutomationTogglePattern>(UIA_PatternIds.UIA_TogglePatternId);
            if (togglePattern != null)
            {
                RetryIfNotAvailable(
                    pattern => pattern.Toggle(),
                    togglePattern);
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the TogglePattern.");
            }
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
