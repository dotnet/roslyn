// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using UIAutomationClient;
using AutomationElementIdentifiers = System.Windows.Automation.AutomationElementIdentifiers;
using ExpandCollapsePatternIdentifiers = System.Windows.Automation.ExpandCollapsePatternIdentifiers;
using InvokePatternIdentifiers = System.Windows.Automation.InvokePatternIdentifiers;
using SelectionItemPatternIdentifiers = System.Windows.Automation.SelectionItemPatternIdentifiers;
using TogglePatternIdentifiers = System.Windows.Automation.TogglePatternIdentifiers;
using ValuePatternIdentifiers = System.Windows.Automation.ValuePatternIdentifiers;
using ControlType = System.Windows.Automation.ControlType;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class AutomationElementExtensions
    {
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
            var child = parent.FindFirst(TreeScope.TreeScope_Descendants, condition);

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
            var child = parent.FindFirst(TreeScope.TreeScope_Descendants, condition);

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
            var child = parent.FindFirst(TreeScope.TreeScope_Descendants, condition);

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

        /// <summary>
        /// Invokes an <see cref="IUIAutomationElement"/>.
        /// Throws an <see cref="InvalidOperationException"/> if <paramref name="element"/> does not
        /// support the <see cref="IUIAutomationInvokePattern"/>.
        /// </summary>
        public static void Invoke(this IUIAutomationElement element)
        {
            if (element.GetCurrentPattern(InvokePatternIdentifiers.Pattern.Id) is IUIAutomationInvokePattern invokePattern)
            {
                invokePattern.Invoke();
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
            if (element.GetCurrentPattern(ExpandCollapsePatternIdentifiers.Pattern.Id) is IUIAutomationExpandCollapsePattern expandCollapsePattern)
            {
                expandCollapsePattern.Expand();
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
            if (element.GetCurrentPattern(ExpandCollapsePatternIdentifiers.Pattern.Id) is IUIAutomationExpandCollapsePattern expandCollapsePattern)
            {
                expandCollapsePattern.Collapse();
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
            if (element.GetCurrentPattern(SelectionItemPatternIdentifiers.Pattern.Id) is IUIAutomationSelectionItemPattern selectionItemPattern)
            {
                selectionItemPattern.Select();
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
            if (element.GetCurrentPattern(ValuePatternIdentifiers.Pattern.Id) is IUIAutomationValuePattern valuePattern)
            {
                return valuePattern.CurrentValue;
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
            if (element.GetCurrentPattern(ValuePatternIdentifiers.Pattern.Id) is IUIAutomationValuePattern valuePattern)
            {
                valuePattern.SetValue(value);
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
            if (element.GetCurrentPattern(TogglePatternIdentifiers.Pattern.Id) is IUIAutomationTogglePattern togglePattern)
            {
                return togglePattern.CurrentToggleState == ToggleState.ToggleState_On;
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
            if (element.GetCurrentPattern(TogglePatternIdentifiers.Pattern.Id) is IUIAutomationTogglePattern togglePattern)
            {
                togglePattern.Toggle();
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
            return element.CurrentAutomationId ?? element.CurrentName ?? "<unnamed>";
        }
    }
}
