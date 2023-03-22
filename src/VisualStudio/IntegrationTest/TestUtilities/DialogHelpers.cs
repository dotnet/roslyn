// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UIAutomationClient;
using AutomationElementIdentifiers = System.Windows.Automation.AutomationElementIdentifiers;
using AutomationProperty = System.Windows.Automation.AutomationProperty;
using ControlType = System.Windows.Automation.ControlType;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class DialogHelpers
    {
        /// <summary>
        /// Returns an <see cref="IUIAutomationElement"/> representing the open dialog with automation ID
        /// <paramref name="dialogAutomationId"/>.
        /// Throws an <see cref="InvalidOperationException"/> if an open dialog with that name cannot be
        /// found.
        /// </summary>
        public static IUIAutomationElement GetOpenDialogById(IntPtr visualStudioHWnd, string dialogAutomationId)
        {
            var dialogAutomationElement = FindDialogByAutomationId(visualStudioHWnd, dialogAutomationId, isOpen: true);
            if (dialogAutomationElement == null)
            {
                throw new InvalidOperationException($"Expected the {dialogAutomationId} dialog to be open, but it is not.");
            }

            return dialogAutomationElement;
        }

        public static IUIAutomationElement FindDialogByAutomationId(IntPtr visualStudioHWnd, string dialogAutomationId, bool isOpen, bool wait = true)
        {
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                return Retry(
                    _ => FindDialogWorker(visualStudioHWnd, dialogAutomationId),
                    stoppingCondition: (automationElement, _) => !wait || (isOpen ? automationElement != null : automationElement == null),
                    delay: TimeSpan.FromMilliseconds(250),
                    cancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// Used to find legacy dialogs that don't have an AutomationId
        /// </summary>
        public static IUIAutomationElement FindDialogByName(IntPtr visualStudioHWnd, string dialogName, bool isOpen, CancellationToken cancellationToken)
        {
            return Retry(
                _ => FindDialogByNameWorker(visualStudioHWnd, dialogName),
                stoppingCondition: (automationElement, _) => isOpen ? automationElement != null : automationElement == null,
                delay: TimeSpan.FromMilliseconds(250),
                cancellationToken);
        }

        /// <summary>
        /// Presses the specified button.
        /// The button is identified using its automation ID; see <see cref="PressButtonWithName(IntPtr, string, string)"/>
        /// for the equivalent method that finds the button by name.
        /// </summary>
        public static void PressButton(IntPtr visualStudioHWnd, string dialogAutomationId, string buttonAutomationId)
        {
            var dialogAutomationElement = GetOpenDialogById(visualStudioHWnd, dialogAutomationId);

            var buttonAutomationElement = dialogAutomationElement.FindDescendantByAutomationId(buttonAutomationId);
            buttonAutomationElement.Invoke();
        }

        /// <summary>
        /// Presses the specified button.
        /// The button is identified using its name; see <see cref="PressButton(IntPtr, string, string)"/>
        /// for the equivalent methods that finds the button by automation ID.
        /// </summary>
        public static void PressButtonWithName(IntPtr visualStudioHWnd, string dialogAutomationId, string buttonName)
        {
            var dialogAutomationElement = GetOpenDialogById(visualStudioHWnd, dialogAutomationId);

            var buttonAutomationElement = dialogAutomationElement.FindDescendantByName(buttonName);
            buttonAutomationElement.Invoke();
        }

        /// <summary>
        /// Presses the specified button from a legacy dialog that has no AutomationId.
        /// The button is identified using its name; see <see cref="PressButton(IntPtr, string, string)"/>
        /// for the equivalent methods that finds the button by automation ID.
        /// </summary>
        public static void PressButtonWithNameFromDialogWithName(IntPtr visualStudioHWnd, string dialogName, string buttonName)
        {
            IUIAutomationElement dialogAutomationElement;
            using (var cancellationTokenSource = new CancellationTokenSource(Helper.HangMitigatingTimeout))
            {
                dialogAutomationElement = FindDialogByName(visualStudioHWnd, dialogName, isOpen: true, cancellationTokenSource.Token);
            }

            var buttonAutomationElement = dialogAutomationElement.FindDescendantByName(buttonName);
            buttonAutomationElement.Invoke();
        }

        private static IUIAutomationElement FindDialogWorker(IntPtr visualStudioHWnd, string dialogAutomationName)
            => FindDialogByPropertyWorker(visualStudioHWnd, dialogAutomationName, AutomationElementIdentifiers.AutomationIdProperty);

        private static IUIAutomationElement FindDialogByNameWorker(IntPtr visualStudioHWnd, string dialogName)
            => FindDialogByPropertyWorker(visualStudioHWnd, dialogName, AutomationElementIdentifiers.NameProperty);

        private static IUIAutomationElement FindDialogByPropertyWorker(
            IntPtr visualStudioHWnd,
            string propertyValue,
            AutomationProperty nameProperty)
        {
            var vsAutomationElement = Helper.Automation.ElementFromHandle(visualStudioHWnd);

            var elementCondition = Helper.Automation.CreateAndConditionFromArray(
                new[]
                {
                    Helper.Automation.CreatePropertyCondition(nameProperty.Id, propertyValue),
                    Helper.Automation.CreatePropertyCondition(AutomationElementIdentifiers.ControlTypeProperty.Id, ControlType.Window.Id),
                });

            return vsAutomationElement.FindFirst(TreeScope.TreeScope_Children, elementCondition);
        }

        private static T Retry<T>(Func<CancellationToken, T> action, Func<T, CancellationToken, bool> stoppingCondition, TimeSpan delay, CancellationToken cancellationToken)
        {
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                T retval;
                try
                {
                    retval = action(cancellationToken);
                }
                catch (COMException)
                {
                    // Devenv can throw COMExceptions if it's busy when we make DTE calls.
                    Task.Delay(delay, cancellationToken).GetAwaiter().GetResult();
                    continue;
                }

                if (stoppingCondition(retval, cancellationToken))
                {
                    return retval;
                }
                else
                {
                    Task.Delay(delay, cancellationToken).GetAwaiter().GetResult();
                }
            }
            while (true);
        }
    }
}
