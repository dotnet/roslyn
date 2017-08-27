// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Windows.Automation;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class AutomationElementExtensions
    {
        /// <summary>
        /// Given an <see cref="AutomationElement"/>, returns a descendant with the automation ID specified by <paramref name="automationId"/>.
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
        /// Given an <see cref="AutomationElement"/>, returns a descendant with the automation ID specified by <paramref name="name"/>.
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
        /// Given an <see cref="AutomationElement"/>, returns a descendant with the className specified by <paramref name="className"/>.
        /// Throws an <see cref="InvalidOperationException"/> if no such descendant is found.
        /// </summary>
        public static AutomationElement FindDescendantByClass(this AutomationElement parent, string className)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var condition = new PropertyCondition(AutomationElement.ClassNameProperty, className);
            var child = parent.FindFirst(TreeScope.Descendants, condition);

            if (child == null)
            {
                throw new InvalidOperationException($"Could not find item with class '{className}' under '{parent.GetNameForExceptionMessage()}'.");
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
        /// Gets the value of the given <see cref="AutomationElement"/>.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="ValuePattern"/>.
        /// </summary>
        public static string GetValue(this AutomationElement element)
        {
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
            {
                return (valuePattern as ValuePattern).Current.Value;
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the ValuePattern.");
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
        /// Given an <see cref="AutomationElement"/>, returns a descendent following the <paramref name="path"/>.
        /// Throws an <see cref="InvalidOperationException"/> if no such descendant is found.
        /// </summary>

        public static AutomationElement FindDescendantByPath(this AutomationElement element, string path)
        {
            string[] pathParts = path.Split(".".ToCharArray());

            // traverse the path
            AutomationElement item = element;
            AutomationElement next = null;

            foreach (string pathPart in pathParts)
            {
                next = item.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.LocalizedControlTypeProperty, pathPart));
                
                if (next == null)
                {
                    ThrowUnableToFindChildException(path, item);
                }

                item = next;
            }

            return item;
        }

        private static void ThrowUnableToFindChildException(string path, AutomationElement item)
        {
            // if not found, build a list of available children for debugging purposes
            var validChildren = new List<string>();

            try
            {
                foreach (AutomationElement sub in item.CachedChildren)
                {
                    validChildren.Add(SimpleControlTypeName(sub));
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

        private static string SimpleControlTypeName(AutomationElement element)
        {
            ControlType type = element.GetCurrentPropertyValue(AutomationElement.ControlTypeProperty, true) as ControlType;
            return type == null ? null : type.LocalizedControlType;
        }

        /// <summary>
        /// Returns true if the given <see cref="AutomationElement"/> is in the <see cref="ToggleState.On"/> state.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="TogglePattern"/>.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static bool IsToggledOn(this AutomationElement element)
        {
            if (element.TryGetCurrentPattern(TogglePattern.Pattern, out var togglePattern))
            {
                return (togglePattern as TogglePattern).Current.ToggleState == ToggleState.On;
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the TogglePattern.");
            }
        }

        /// <summary>
        /// Cycles through the <see cref="ToggleState"/>s of the given <see cref="AutomationElement"/>.
        /// </summary>
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="TogglePattern"/>.
        public static void Toggle(this AutomationElement element)
        {
            if (element.TryGetCurrentPattern(TogglePattern.Pattern, out var togglePattern))
            {
                (togglePattern as TogglePattern).Toggle();
            }
            else
            {
                throw new InvalidOperationException($"The element '{element.GetNameForExceptionMessage()}' does not support the TogglePattern.");
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
