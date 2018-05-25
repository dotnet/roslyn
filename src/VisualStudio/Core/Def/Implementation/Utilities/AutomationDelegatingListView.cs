// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias slowautomation;

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using slowautomation::System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal class AutomationDelegatingListView : ListView
    {
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is AutomationDelegatingListViewItem;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new AutomationDelegatingListViewItem();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new AutomationDelegatingListViewAutomationPeer(this);
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            if (this.SelectedIndex == -1)
            {
                this.SelectedIndex = 0;
            }
        }
    }

    internal class AutomationDelegatingListViewAutomationPeer : FrameworkElementAutomationPeer
    {
        public AutomationDelegatingListViewAutomationPeer(AutomationDelegatingListView listView)
            : base(listView)
        {
        }

        protected override List<AutomationPeer> GetChildrenCore()
        {
            var results = new List<AutomationPeer>();
            var peersToProcess = new Queue<AutomationPeer>(base.GetChildrenCore() ?? SpecializedCollections.EmptyEnumerable<AutomationPeer>());
            while (peersToProcess.Count > 0)
            {
                var peer = peersToProcess.Dequeue();
                if (peer is ListBoxItemWrapperAutomationPeer itemWrapperAutomationPeer)
                {
                    results.Add(itemWrapperAutomationPeer);
                }
                else
                {
                    foreach (var childPeer in peer.GetChildren() ?? SpecializedCollections.EmptyEnumerable<AutomationPeer>())
                    {
                        peersToProcess.Enqueue(childPeer);
                    }
                }
            }
            
            return results;
        }
    }

    internal class AutomationDelegatingListViewItem : ListViewItem
    {
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new AutomationDelegatingListViewItemAutomationPeer(this);
        }
    }

    internal class AutomationDelegatingListViewItemAutomationPeer : ListBoxItemWrapperAutomationPeer
    {
        private CheckBoxAutomationPeer checkBoxItem;
        private RadioButtonAutomationPeer radioButtonItem;
        private TextBlockAutomationPeer textBlockItem;

        public AutomationDelegatingListViewItemAutomationPeer(AutomationDelegatingListViewItem listViewItem)
            : base(listViewItem) 
        {
            checkBoxItem = this.GetChildren().OfType<CheckBoxAutomationPeer>().SingleOrDefault();
            if (checkBoxItem != null)
            {
                var toggleButton = ((CheckBox)checkBoxItem.Owner);
                toggleButton.Checked += Checkbox_CheckChanged;
                toggleButton.Unchecked += Checkbox_CheckChanged;
                return;
            }

            radioButtonItem = this.GetChildren().OfType<RadioButtonAutomationPeer>().SingleOrDefault();
            if (radioButtonItem != null)
            {
                var toggleButton = ((RadioButton)radioButtonItem.Owner);
                toggleButton.Checked +=   RadioButton_CheckChanged;
                toggleButton.Unchecked += RadioButton_CheckChanged;
                return;
            }

            textBlockItem = this.GetChildren().OfType<TextBlockAutomationPeer>().FirstOrDefault();
        }

        private void Checkbox_CheckChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            RaisePropertyChangedEvent(
                TogglePatternIdentifiers.ToggleStateProperty, 
                oldValue: ConvertToToggleState(!checkBox.IsChecked),
                newValue: ConvertToToggleState(checkBox.IsChecked));
        }

        private void RadioButton_CheckChanged(object sender, RoutedEventArgs e)
        {
            // RadioButtonAutomationPeer sets oldValue and newValue to true, so we do the same here
            // See http://referencesource.microsoft.com/#PresentationFramework/src/Framework/System/Windows/Automation/Peers/RadioButtonAutomationPeer.cs,114
            RaisePropertyChangedEvent(
                SelectionItemPatternIdentifiers.IsSelectedProperty,
                oldValue: true,
                newValue: true);
        }

        private static ToggleState ConvertToToggleState(bool? value)
        {
            switch (value)
            {
                case true: return ToggleState.On;
                case false: return ToggleState.Off;
                default: return ToggleState.Indeterminate;
            }
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            if (checkBoxItem != null)
            {
                return AutomationControlType.CheckBox;
            }
            else if (radioButtonItem != null)
            {
                return AutomationControlType.RadioButton;
            }
            else
            {
                return AutomationControlType.Text;
            }
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            var automationPeer = GetAutomationPeer();
            return automationPeer != null
                ? automationPeer.GetPattern(patternInterface)
                : base.GetPattern(patternInterface);
        }

        protected override string GetNameCore()
        {
            return GetAutomationPeer()?.GetName() ?? string.Empty;
        }

        private AutomationPeer GetAutomationPeer()
        {
            return checkBoxItem ?? radioButtonItem ?? (AutomationPeer)textBlockItem;
        }
    }
}
