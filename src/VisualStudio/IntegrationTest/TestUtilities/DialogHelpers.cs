// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class DialogHelpers
    {
        /// <summary>
        /// Returns an <see cref="AutomationElement"/> representing the open dialog with automation ID
        /// <paramref name="dialogAutomationId"/>.
        /// Throws an <see cref="InvalidOperationException"/> if an open dialog with that name cannot be
        /// found.
        /// </summary>
        public static AutomationElement GetOpenDialogById(int visualStudioHWnd, string dialogAutomationId)
        {
            var dialogAutomationElement = FindDialogByAutomationId(visualStudioHWnd, dialogAutomationId, isOpen: true);
            if (dialogAutomationElement == null)
            {
                throw new InvalidOperationException($"Expected the {dialogAutomationId} dialog to be open, but it is not.");
            }

            return dialogAutomationElement;
        }

        public static AutomationElement FindDialogByAutomationId(int visualStudioHWnd, string dialogAutomationId, bool isOpen, bool wait = true)
        {
            return Retry(
                () => FindDialogWorker(visualStudioHWnd, dialogAutomationId),
                stoppingCondition: automationElement => !wait || (isOpen ? automationElement != null : automationElement == null),
                delay: TimeSpan.FromMilliseconds(250));
        }

        /// <summary>
        /// Used to find legacy dialogs that don't have an AutomationId
        /// </summary>
        public static AutomationElement FindDialogByName(int visualStudioHWnd, string dialogName, bool isOpen)
        {
            return Retry(
                () => FindDialogByNameWorker(visualStudioHWnd, dialogName),
                stoppingCondition: automationElement => isOpen ? automationElement != null : automationElement == null,
                delay: TimeSpan.FromMilliseconds(250));
        }

        /// <summary>
        /// Selects a specific item in a combo box.
        /// Note that combo box is found using its Automation ID, but the item is identified by name.
        /// </summary>
        public static void SelectComboBoxItem(int visualStudioHWnd, string dialogAutomationName, string comboBoxAutomationName, string itemText)
        {
            var dialogAutomationElement = GetOpenDialogById(visualStudioHWnd, dialogAutomationName);

            var comboBoxAutomationElement = dialogAutomationElement.FindDescendantByAutomationId(comboBoxAutomationName);
            comboBoxAutomationElement.Expand();

            var comboBoxItemAutomationElement = comboBoxAutomationElement.FindDescendantByName(itemText);
            comboBoxItemAutomationElement.Select();

            comboBoxAutomationElement.Collapse();
        }

        /// <summary>
        /// Selects a specific radio button from a dialog found by Id.
        /// </summary>
        public static void SelectRadioButton(int visualStudioHWnd, string dialogAutomationName, string radioButtonAutomationName)
        {
            var dialogAutomationElement = GetOpenDialogById(visualStudioHWnd, dialogAutomationName);

            var radioButton = dialogAutomationElement.FindDescendantByAutomationId(radioButtonAutomationName);
            radioButton.Select();
        }

        /// <summary>
        /// Sets the value of the specified element in the dialog.
        /// Used for setting the values of things like combo boxes and text fields.
        /// </summary>
        public static void SetElementValue(int visualStudioHWnd, string dialogAutomationId, string elementAutomationId, string value)
        {
            var dialogAutomationElement = GetOpenDialogById(visualStudioHWnd, dialogAutomationId);

            var control = dialogAutomationElement.FindDescendantByAutomationId(elementAutomationId);
            control.SetValue(value);
        }

        /// <summary>
        /// Presses the specified button.
        /// The button is identified using its automation ID; see <see cref="PressButtonWithName(int, string, string)"/>
        /// for the equivalent method that finds the button by name.
        /// </summary>
        public static void PressButton(int visualStudioHWnd, string dialogAutomationId, string buttonAutomationId)
        {
            var dialogAutomationElement = GetOpenDialogById(visualStudioHWnd, dialogAutomationId);

            var buttonAutomationElement = dialogAutomationElement.FindDescendantByAutomationId(buttonAutomationId);
            buttonAutomationElement.Invoke();
        }

        /// <summary>
        /// Presses the specified button.
        /// The button is identified using its name; see <see cref="PressButton(int, string, string)"/>
        /// for the equivalent methods that finds the button by automation ID.
        /// </summary>
        public static void PressButtonWithName(int visualStudioHWnd, string dialogAutomationId, string buttonName)
        {
            var dialogAutomationElement = GetOpenDialogById(visualStudioHWnd, dialogAutomationId);

            var buttonAutomationElement = dialogAutomationElement.FindDescendantByName(buttonName);
            buttonAutomationElement.Invoke();
        }

        /// <summary>
        /// Presses the specified button from a legacy dialog that has no AutomationId.
        /// The button is identified using its name; see <see cref="PressButton(int, string, string)"/>
        /// for the equivalent methods that finds the button by automation ID.
        /// </summary>
        public static void PressButtonWithNameFromDialogWithName(int visualStudioHWnd, string dialogName, string buttonName)
        {
            var dialogAutomationElement = FindDialogByName(visualStudioHWnd, dialogName, isOpen: true);

            var buttonAutomationElement = dialogAutomationElement.FindDescendantByName(buttonName);
            buttonAutomationElement.Invoke();
        }

        private static AutomationElement FindDialogWorker(int visualStudioHWnd, string dialogAutomationName)
            => FindDialogByPropertyWorker(visualStudioHWnd, dialogAutomationName, AutomationElement.AutomationIdProperty);

        private static AutomationElement FindDialogByNameWorker(int visualStudioHWnd, string dialogName)
            => FindDialogByPropertyWorker(visualStudioHWnd, dialogName, AutomationElement.NameProperty);

        private static AutomationElement FindDialogByPropertyWorker(
            int visualStudioHWnd, 
            string propertyValue, 
            AutomationProperty nameProperty)
        {
            var vsAutomationElement = AutomationElement.FromHandle(new IntPtr(visualStudioHWnd));

            Condition elementCondition = new AndCondition(
                new PropertyCondition(nameProperty, propertyValue),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

            return vsAutomationElement.FindFirst(TreeScope.Descendants, elementCondition);
        }

        private static T Retry<T>(Func<T> action, Func<T, bool> stoppingCondition, TimeSpan delay)
        {
            DateTime beginTime = DateTime.UtcNow;
            T retval = default(T);

            do
            {
                try
                {
                    retval = action();
                }
                catch (ElementNotAvailableException)
                {
                    // Devenv can throw automation exceptions if it's busy when we make DTE calls.
                    Thread.Sleep(delay);
                    continue;
                }
                catch (COMException)
                {
                    // Devenv can throw COMExceptions if it's busy when we make DTE calls.
                    Thread.Sleep(delay);
                    continue;
                }

                if (stoppingCondition(retval))
                {
                    return retval;
                }
                else
                {
                    Thread.Sleep(delay);
                }
            }
            while (true);
        }
    }
}
