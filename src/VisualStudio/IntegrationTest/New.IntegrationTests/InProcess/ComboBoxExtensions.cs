// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Input
{
    public static class ComboBoxExtensions
    {
        public static Task<bool> SimulateSelectItemAsync(this ComboBox comboBox, JoinableTaskFactory joinableTaskFactory, string itemText, CancellationToken cancellationToken)
            => SimulateSelectItemAsync(comboBox, joinableTaskFactory, itemText, mustExist: true, cancellationToken);

        public static async Task<bool> SimulateSelectItemAsync(this ComboBox comboBox, JoinableTaskFactory joinableTaskFactory, string itemText, bool mustExist, CancellationToken cancellationToken)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (comboBox.Text == itemText)
            {
                return true;
            }

            if (!await comboBox.SimulateExpandAsync(joinableTaskFactory, cancellationToken))
            {
                return false;
            }

            var peer = new ComboBoxAutomationPeer(comboBox);

            var children = peer.GetChildren()?.OfType<ListBoxItemAutomationPeer>().ToList();
            var existingItem = children?.Find(x => x.GetName() == itemText);
            if (existingItem is null)
            {
                if (mustExist)
                {
                    throw new InvalidOperationException($"Item '{itemText}' was not found in the combo box.");
                }

                // Collapse the combo box, and then set the value explicitly
                if (!await comboBox.SimulateCollapseAsync(joinableTaskFactory, cancellationToken)
                    || !await comboBox.SimulateSetTextAsync(joinableTaskFactory, itemText, cancellationToken))
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

                if (!await comboBox.SimulateCollapseAsync(joinableTaskFactory, cancellationToken))
                {
                    return false;
                }

                return true;
            }
        }

        public static async Task<bool> SimulateExpandAsync(this ComboBox comboBox, JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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

        public static async Task<bool> SimulateCollapseAsync(this ComboBox comboBox, JoinableTaskFactory joinableTaskFactory, CancellationToken cancellationToken)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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

        public static async Task<bool> SimulateSetTextAsync(this ComboBox comboBox, JoinableTaskFactory joinableTaskFactory, string value, CancellationToken cancellationToken)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            await joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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
