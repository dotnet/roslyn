// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    public static class DialogHelpers
    {
        /// <summary>
        /// Returns an <see cref="AutomationElement"/> representing the open dialog with automation ID
        /// <paramref name="dialogAutomationName"/>.
        /// Throws an <see cref="InvalidOperationException"/> if an open dialog with that name cannot be
        /// found.
        /// </summary>
        public static AutomationElement GetOpenDialog(int visualStudioHWnd, string dialogAutomationName)
        {
            var dialogAutomationElement = FindDialog(visualStudioHWnd, dialogAutomationName, isOpen: true);
            if (dialogAutomationElement == null)
            {
                throw new InvalidOperationException($"Expected the {dialogAutomationName} dialog to be open, but it is not.");
            }

            return dialogAutomationElement;
        }

        public static AutomationElement FindDialog(int visualStudioHWnd, string dialogAutomationName, bool isOpen)
        {
            return Retry(
                () => FindDialogWorker(visualStudioHWnd, dialogAutomationName),
                stoppingCondition: automationElement => isOpen ? automationElement != null : automationElement == null,
                delay: TimeSpan.FromMilliseconds(250));
        }

        /// <summary>
        /// Selects a specific item in a combo box.
        /// Note that combo box is found using its Automation ID, but the item is identified by name.
        /// </summary>
        public static void SelectComboBoxItem(int visualStudioHWnd, string dialogAutomationName, string comboBoxAutomationName, string itemText)
        {
            var dialogAutomationElement = GetOpenDialog(visualStudioHWnd, dialogAutomationName);

            var comboBoxAutomationElement = dialogAutomationElement.FindDescendantByAutomationId(comboBoxAutomationName);
            comboBoxAutomationElement.Expand();

            var comboBoxItemAutomationElement = comboBoxAutomationElement.FindDescendantByName(itemText);
            comboBoxItemAutomationElement.Select();

            comboBoxAutomationElement.Collapse();
        }

        /// <summary>
        /// Selects a specific radio button.
        /// </summary>
        public static void SelectRadioButton(int visualStudioHWnd, string dialogAutomationName, string radioButtonAutomationName)
        {
            var dialogAutomationElement = GetOpenDialog(visualStudioHWnd, dialogAutomationName);

            var radioButton = dialogAutomationElement.FindDescendantByAutomationId(radioButtonAutomationName);
            radioButton.Select();
        }

        /// <summary>
        /// Sets the value of the specified element in the dialog.
        /// Used for setting the values of things like combo boxes and text fields.
        /// </summary>
        public static void SetElementValue(int visualStudioHWnd, string dialogAutomationName, string elementAutomationName, string value)
        {
            var dialogAutomationElement = GetOpenDialog(visualStudioHWnd, dialogAutomationName);

            var control = dialogAutomationElement.FindDescendantByAutomationId(elementAutomationName);
            control.SetValue(value);
        }

        /// <summary>
        /// Presses the specified button.
        /// The button is identified using its automation ID; see <see cref="PressButtonWithName(int, string, string)"/>
        /// for the equivalent method that finds the button by name.
        /// </summary>
        public static void PressButton(int visualStudioHWnd, string dialogAutomationName, string buttonAutomationName)
        {
            var dialogAutomationElement = GetOpenDialog(visualStudioHWnd, dialogAutomationName);

            var buttonAutomationElement = dialogAutomationElement.FindDescendantByAutomationId(buttonAutomationName);
            buttonAutomationElement.Invoke();
        }

        /// <summary>
        /// Presses the specified button.
        /// The button is identified using its name; see <see cref="PressButton(int, string, string)"/>
        /// for the equivalent methods that finds the button by automation ID.
        /// </summary>
        public static void PressButtonWithName(int visualStudioHWnd, string dialogAutomationName, string buttonName)
        {
            var dialogAutomationElement = GetOpenDialog(visualStudioHWnd, dialogAutomationName);

            var buttonAutomationElement = dialogAutomationElement.FindDescendantByName(buttonName);
            buttonAutomationElement.Invoke();
        }

        private static AutomationElement FindDialogWorker(int visualStudioHWnd, string dialogAutomationName)
        {
            var vsAutomationElement = AutomationElement.FromHandle(new IntPtr(visualStudioHWnd));

            Condition elementCondition = new AndCondition(
                new PropertyCondition(AutomationElement.AutomationIdProperty, dialogAutomationName),
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
