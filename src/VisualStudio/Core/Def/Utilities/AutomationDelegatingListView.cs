// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias slowautomation;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using slowautomation::System.Windows.Automation;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

internal sealed class AutomationDelegatingListView : ListView
{
    protected override bool IsItemItsOwnContainerOverride(object item)
        => item is AutomationDelegatingListViewItem;

    protected override DependencyObject GetContainerForItemOverride()
        => new AutomationDelegatingListViewItem();

    protected override AutomationPeer OnCreateAutomationPeer()
        => new AutomationDelegatingListViewAutomationPeer(this);

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        if (this.SelectedIndex == -1)
        {
            this.SelectedIndex = 0;
        }
    }
}

internal sealed class AutomationDelegatingListViewAutomationPeer : FrameworkElementAutomationPeer
{
    public AutomationDelegatingListViewAutomationPeer(AutomationDelegatingListView listView)
        : base(listView)
    {
    }

    protected override List<AutomationPeer>? GetChildrenCore()
    {
        List<AutomationPeer>? results = null;
        var peersToProcess = new Queue<AutomationPeer>(base.GetChildrenCore() ?? SpecializedCollections.EmptyEnumerable<AutomationPeer>());
        while (peersToProcess.Count > 0)
        {
            var peer = peersToProcess.Dequeue();
            if (peer is ListBoxItemWrapperAutomationPeer itemWrapperAutomationPeer)
            {
                results ??= [];
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

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.List;
}

internal sealed class AutomationDelegatingListViewItem : ListViewItem
{
    protected override AutomationPeer OnCreateAutomationPeer()
        => new AutomationDelegatingListViewItemAutomationPeer(this);
}

internal sealed class AutomationDelegatingListViewItemAutomationPeer : ListBoxItemWrapperAutomationPeer
{
    private readonly CheckBoxAutomationPeer? checkBoxItem;
    private readonly RadioButtonAutomationPeer? radioButtonItem;
    private readonly TextBlockAutomationPeer? textBlockItem;

    public AutomationDelegatingListViewItemAutomationPeer(AutomationDelegatingListViewItem listViewItem)
        : base(listViewItem)
    {
        checkBoxItem = this.GetChildren()?.OfType<CheckBoxAutomationPeer>().SingleOrDefault();
        if (checkBoxItem != null)
        {
            var toggleButton = ((CheckBox)checkBoxItem.Owner);
            toggleButton.Checked += Checkbox_CheckChanged;
            toggleButton.Unchecked += Checkbox_CheckChanged;
            return;
        }

        radioButtonItem = this.GetChildren()?.OfType<RadioButtonAutomationPeer>().SingleOrDefault();
        if (radioButtonItem != null)
        {
            var toggleButton = ((RadioButton)radioButtonItem.Owner);
            toggleButton.Checked += RadioButton_CheckChanged;
            toggleButton.Unchecked += RadioButton_CheckChanged;
            return;
        }

        textBlockItem = this.GetChildren()?.OfType<TextBlockAutomationPeer>().FirstOrDefault();
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

    public override object? GetPattern(PatternInterface patternInterface)
    {
        var automationPeer = GetAutomationPeer();
        return automationPeer != null
            ? automationPeer.GetPattern(patternInterface)
            : base.GetPattern(patternInterface);
    }

    protected override string GetNameCore()
        => GetAutomationPeer()?.GetName() ?? string.Empty;

    private AutomationPeer? GetAutomationPeer()
        => checkBoxItem ?? radioButtonItem ?? (AutomationPeer?)textBlockItem;
}
