// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using UIAutomationClient;
using AutomationElementIdentifiers = System.Windows.Automation.AutomationElementIdentifiers;
using AutomationProperty = System.Windows.Automation.AutomationProperty;
using ControlType = System.Windows.Automation.ControlType;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess;

public static class DialogHelpers
{
    /// <summary>
    /// Returns an <see cref="IUIAutomationElement"/> representing the open dialog with automation ID
    /// <paramref name="dialogAutomationId"/>.
    /// Throws an <see cref="InvalidOperationException"/> if an open dialog with that name cannot be
    /// found.
    /// </summary>
    public static async Task<IUIAutomationElement> GetOpenDialogByIdAsync(JoinableTaskFactory joinableTaskFactory, IntPtr visualStudioHWnd, string dialogAutomationId, CancellationToken cancellationToken)
    {
        var dialogAutomationElement = await FindDialogByAutomationIdAsync(joinableTaskFactory, visualStudioHWnd, dialogAutomationId, isOpen: true, cancellationToken);
        if (dialogAutomationElement == null)
        {
            throw new InvalidOperationException($"Expected the {dialogAutomationId} dialog to be open, but it is not.");
        }

        return dialogAutomationElement;
    }

    public static async Task<IUIAutomationElement> FindDialogByAutomationIdAsync(JoinableTaskFactory joinableTaskFactory, IntPtr visualStudioHWnd, string dialogAutomationId, bool isOpen, CancellationToken cancellationToken)
    {
        return await RetryAsync(
            joinableTaskFactory,
            _ => FindDialogWorker(visualStudioHWnd, dialogAutomationId),
            stoppingCondition: (automationElement, _) => isOpen ? automationElement != null : automationElement == null,
            delay: TimeSpan.FromMilliseconds(250),
            cancellationToken);
    }

    /// <summary>
    /// Used to find legacy dialogs that don't have an AutomationId
    /// </summary>
    public static async Task<IUIAutomationElement> FindDialogByNameAsync(JoinableTaskFactory joinableTaskFactory, IntPtr visualStudioHWnd, string dialogName, bool isOpen, CancellationToken cancellationToken)
    {
        return await RetryAsync(
            joinableTaskFactory,
            _ => FindDialogByNameWorker(visualStudioHWnd, dialogName),
            stoppingCondition: (automationElement, _) => isOpen ? automationElement != null : automationElement == null,
            delay: TimeSpan.FromMilliseconds(250),
            cancellationToken);
    }

    /// <summary>
    /// Presses the specified button.
    /// The button is identified using its automation ID; see <see cref="PressButtonWithNameAsync(JoinableTaskFactory, IntPtr, string, string, CancellationToken)"/>
    /// for the equivalent method that finds the button by name.
    /// </summary>
    public static async Task PressButtonAsync(JoinableTaskFactory joinableTaskFactory, IntPtr visualStudioHWnd, string dialogAutomationId, string buttonAutomationId, CancellationToken cancellationToken)
    {
        var dialogAutomationElement = await GetOpenDialogByIdAsync(joinableTaskFactory, visualStudioHWnd, dialogAutomationId, cancellationToken);

        var buttonAutomationElement = dialogAutomationElement.FindDescendantByAutomationId(buttonAutomationId);
        buttonAutomationElement.Invoke();
    }

    /// <summary>
    /// Presses the specified button.
    /// The button is identified using its name; see <see cref="PressButtonAsync(JoinableTaskFactory, IntPtr, string, string, CancellationToken)"/>
    /// for the equivalent methods that finds the button by automation ID.
    /// </summary>
    public static async Task PressButtonWithNameAsync(JoinableTaskFactory joinableTaskFactory, IntPtr visualStudioHWnd, string dialogAutomationId, string buttonName, CancellationToken cancellationToken)
    {
        var dialogAutomationElement = await GetOpenDialogByIdAsync(joinableTaskFactory, visualStudioHWnd, dialogAutomationId, cancellationToken);

        var buttonAutomationElement = dialogAutomationElement.FindDescendantByName(buttonName);
        buttonAutomationElement.Invoke();
    }

    /// <summary>
    /// Presses the specified button from a legacy dialog that has no AutomationId.
    /// The button is identified using its name; see <see cref="PressButtonAsync(JoinableTaskFactory, IntPtr, string, string, CancellationToken)"/>
    /// for the equivalent methods that finds the button by automation ID.
    /// </summary>
    public static async Task PressButtonWithNameFromDialogWithNameAsync(JoinableTaskFactory joinableTaskFactory, IntPtr visualStudioHWnd, string dialogName, string buttonName, CancellationToken cancellationToken)
    {
        var dialogAutomationElement = await FindDialogByNameAsync(joinableTaskFactory, visualStudioHWnd, dialogName, isOpen: true, cancellationToken);

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

    private static async Task<T> RetryAsync<T>(JoinableTaskFactory joinableTaskFactory, Func<CancellationToken, T> action, Func<T, CancellationToken, bool> stoppingCondition, TimeSpan delay, CancellationToken cancellationToken)
    {
        await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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
                await Task.Delay(delay, cancellationToken);
                continue;
            }

            if (stoppingCondition(retval, cancellationToken))
            {
                return retval;
            }
            else
            {
                await Task.Delay(delay, cancellationToken);
            }
        }
        while (true);
    }
}
