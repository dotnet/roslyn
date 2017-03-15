// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows.Automation;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class AutomationElementExtensions
    {
        /// <summary>
        /// Given an <see cref="AutomationElement"/>, returns a descendent with the automation ID specified by <paramref name="automationId"/>.
        /// Throws an <see cref="InvalidOperationException"/> if no such descendant is found.
        /// </summary>
        public static AutomationElement FindDescendantByAutomationId(this AutomationElement parent, string automationId)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
            var child = parent.FindFirst(TreeScope.Descendants, condition);

            if (child == null)
            {
                throw new InvalidOperationException($"Could not find item with Automation ID '{automationId}' under '{parent.GetNameForExceptionMessage()}'.");
            }

            return child;
        }

        /// <summary>
        /// Given an <see cref="AutomationElement"/>, returns a descendent with the automation ID specified by <paramref name="name"/>.
        /// Throws an <see cref="InvalidOperationException"/> if no such descendant is found.
        /// </summary>
        public static AutomationElement FindDescendantByName(this AutomationElement parent, string name)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var condition = new PropertyCondition(AutomationElement.NameProperty, name);
            var child = parent.FindFirst(TreeScope.Descendants, condition);

            if (child == null)
            {
                throw new InvalidOperationException($"Could not find item with name '{name}' under '{parent.GetNameForExceptionMessage()}'.");
            }

            return child;
        }

        /// <summary>
        /// Given an <see cref="AutomationElement"/>, returns all descendants with the given <paramref name="className"/>.
        /// If none are found, the resulting collection will be empty.
        /// </summary>
        /// <returns></returns>
        public static AutomationElementCollection FindDescendantsByClass(this AutomationElement parent, string className)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var condition = new PropertyCondition(AutomationElement.ClassNameProperty, className);
            return parent.FindAll(TreeScope.Descendants, condition);
        }

        /// <summary>
        /// Invokes an <see cref="AutomationElement"/>.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="InvokePattern"/>.
        /// </summary>
        public static void Invoke(this AutomationElement element)
        {
            if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
            {
                (invokePattern as InvokePattern).Invoke();
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the InvokePattern.");
            }
        }

        /// <summary>
        /// Expands an <see cref="AutomationElement"/>.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="ExpandCollapsePattern"/>.
        /// </summary>
        public static void Expand(this AutomationElement element)
        {
            if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expandCollapsePattern))
            {
                (expandCollapsePattern as ExpandCollapsePattern).Expand();
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the ExpandCollapsePattern.");
            }
        }

        /// <summary>
        /// Collapses an <see cref="AutomationElement"/>.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="ExpandCollapsePattern"/>.
        /// </summary>
        public static void Collapse(this AutomationElement element)
        {
            if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expandCollapsePattern))
            {
                (expandCollapsePattern as ExpandCollapsePattern).Collapse();
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the ExpandCollapsePattern.");
            }
        }

        /// <summary>
        /// Selects an <see cref="AutomationElement"/>.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="SelectionItemPattern"/>.
        /// </summary>
        public static void Select(this AutomationElement element)
        {
            if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionItemPattern))
            {
                (selectionItemPattern as SelectionItemPattern).Select();
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the SelectionItemPattern.");
            }
        }

        /// <summary>
        /// Sets the value of the given <see cref="AutomationElement"/>.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="ValuePattern"/>.
        /// </summary>
        public static void SetValue(this AutomationElement element, string value)
        {
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
            {
                (valuePattern as ValuePattern).SetValue(value);
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the ValuePattern.");
            }
        }

        /// <summary>
        /// Given an <see cref="AutomationElement"/> returns a string representing the "name" of the element, if it has one.
        /// </summary>
        private static string GetNameForExceptionMessage(this AutomationElement element)
        {
            return element.Current.AutomationId ?? element.Current.Name ?? "<unnamed>";
        }
    }
}
