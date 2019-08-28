// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Input
{
    public static class ComboBoxExtensions
    {
        public static async Task<bool> SimulateSelectItemAsync(this ComboBox comboBox, JoinableTaskFactory joinableTaskFactory, string itemText, bool mustExist = true)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync();

            if (comboBox.Text == itemText)
            {
                return true;
            }

            if (!await comboBox.SimulateExpandAsync(joinableTaskFactory))
            {
                return false;
            }

            var peer = new ComboBoxAutomationPeer(comboBox);

            var children = peer.GetChildren().OfType<ListBoxItemAutomationPeer>().ToList();
            var existingItem = children.Find(x => x.GetName() == itemText);
            if (existingItem is null)
            {
                if (mustExist)
                {
                    throw new InvalidOperationException($"Item '{itemText}' was not found in the combo box.");
                }

                // Collapse the combo box, and then set the value explicitly
                if (!await comboBox.SimulateCollapseAsync(joinableTaskFactory)
                    || !await comboBox.SimulateSetTextAsync(joinableTaskFactory, itemText))
                {
                    return false;
                }

                return true;
            }
            else
            {
                ISelectionItemProvider selectionItemProvider = existingItem;
                selectionItemProvider.Select();

                // Wait for changes to propagate
                await Task.Yield();

                if (!await comboBox.SimulateCollapseAsync(joinableTaskFactory))
                {
                    return false;
                }

                return true;
            }
        }

        public static async Task<bool> SimulateExpandAsync(this ComboBox comboBox, JoinableTaskFactory joinableTaskFactory)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync();

            if (comboBox.IsDropDownOpen)
            {
                return true;
            }

            if (!comboBox.IsEnabled || !comboBox.IsVisible)
            {
                return false;
            }

            IExpandCollapseProvider expandCollapseProvider = new ComboBoxAutomationPeer(comboBox);
            expandCollapseProvider.Expand();

            // Wait for changes to propagate
            await Task.Yield();

            return true;
        }

        public static async Task<bool> SimulateCollapseAsync(this ComboBox comboBox, JoinableTaskFactory joinableTaskFactory)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync();

            if (!comboBox.IsDropDownOpen)
            {
                return true;
            }

            if (!comboBox.IsEnabled || !comboBox.IsVisible)
            {
                return false;
            }

            IExpandCollapseProvider expandCollapseProvider = new ComboBoxAutomationPeer(comboBox);
            expandCollapseProvider.Collapse();

            // Wait for changes to propagate
            await Task.Yield();

            return true;
        }

        public static async Task<bool> SimulateSetTextAsync(this ComboBox comboBox, JoinableTaskFactory joinableTaskFactory, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            await joinableTaskFactory.SwitchToMainThreadAsync();

            if (comboBox.Text == value)
            {
                return true;
            }

            if (!comboBox.IsEditable)
            {
                throw new InvalidOperationException("The combo box is not editable.");
            }

            if (!comboBox.IsEnabled || !comboBox.IsVisible)
            {
                return false;
            }

            IValueProvider valueProvider = new ComboBoxAutomationPeer(comboBox);
            valueProvider.SetValue(value);

            // Wait for changes to propagate
            await Task.Yield();

            return true;
        }
    }
}
