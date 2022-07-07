// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Roslyn.VisualStudio.IntegrationTests;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.LanguageServices;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.NewIntegrationTests
{
    public abstract class AbstractAutomationObjectTests : AbstractEditorTest
    {
        protected AbstractAutomationObjectTests() : base(nameof(AbstractAutomationObjectTests))
        {
        }

        [IdeFact]
        public async Task TestOptionsInUIShouldBeInAutomationObject()
        {
            var package = await TestServices.Editor.GetLanguagePackageAsync(LanguageName, HangMitigatingCancellationToken);
            var workspace = await TestServices.Shell.GetComponentModelServiceAsync<VisualStudioWorkspace>(HangMitigatingCancellationToken);
            var automationObject = CreateAutomationObject(workspace);
            foreach (var attribute in package.GetType().GetCustomAttributes(typeof(ProvideLanguageEditorOptionPageAttribute)))
            {
                var pageType = ((ProvideLanguageEditorOptionPageAttribute)attribute).PageType;
                var dialogPage = (AbstractOptionPage)package.GetDialogPage(pageType);
                var optionPageControl = (AbstractOptionPageControl)dialogPage.GetType().GetProperty("Child", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(dialogPage, null);
                var radioButtonGroups = new Dictionary<string, List<RadioButton>>();
                // TODO: Not all options (and even pages) use BindToOption.
                // So the validation here is incomplete.
                foreach (var bindingExpression in optionPageControl.BindingExpressions)
                {
                    var target = (FrameworkElement)bindingExpression.Target;
                    var optionForAssertMessage = target.Name;

                    if (target is CheckBox checkBox)
                    {
                        VerifySingleChangeWhenOptionChangeInUI(dialogPage, automationObject, () => checkBox.IsChecked = !checkBox.IsChecked, optionForAssertMessage);
                    }
                    else if (target is ComboBox comboBox)
                    {
                        VerifySingleChangeWhenOptionChangeInUI(dialogPage, automationObject, () => comboBox.SelectedIndex = comboBox.SelectedIndex == 0 ? 1 : 0, optionForAssertMessage);
                    }
                    else if (target is RadioButton radioButton)
                    {
                        if (radioButtonGroups.TryGetValue(radioButton.GroupName, out var list))
                        {
                            list.Add(radioButton);
                        }
                        else
                        {
                            radioButtonGroups.Add(radioButton.GroupName, new List<RadioButton> { radioButton });
                        }
                    }
                    else
                    {
                        throw ExceptionUtilities.UnexpectedValue(target);
                    }
                }

                foreach (var radioButtonGroup in radioButtonGroups)
                {
                    var groupName = radioButtonGroup.Key;
                    var radioButtons = radioButtonGroup.Value;
                    // There is no point in having a single radio button in a group.
                    Assert.True(radioButtons.Count > 1, $"Expected radio button group '{groupName}' to have more than one radio button. Found {radioButtons.Count}.");
                    var selectedRadioButton = radioButtons.SingleOrDefault(r => r.IsChecked == true);
                    foreach (var radioButton in radioButtons)
                    {
                        // We test selecting every radio button in the group.
                        // We skip the already selected one till we are sure we tested other radio buttons.
                        if (radioButton == selectedRadioButton)
                        {
                            continue;
                        }

                        Assert.False(radioButton.IsChecked);
                        VerifySingleChangeWhenOptionChangeInUI(dialogPage, automationObject, () => radioButton.IsChecked = true, optionForAssertMessage: radioButton.Name);
                    }

                    // TODO: Consider asserting a non-null selectedRadioButton if https://github.com/dotnet/roslyn/issues/62363 is fixed.

                    if (selectedRadioButton is not null)
                    {
                        // Now that we tested other radio buttons in the group, the initially selected radio button is now not selected.
                        Assert.False(selectedRadioButton.IsChecked);
                        VerifySingleChangeWhenOptionChangeInUI(dialogPage, automationObject, () => selectedRadioButton.IsChecked = true, optionForAssertMessage: selectedRadioButton.Name);
                    }
                }
            }

            // Above checks that all options are in AutomationObjects.
            // TODO: check that all automation object members are in options.
        }

        protected abstract AbstractAutomationObject CreateAutomationObject(Workspace workspace);

        private static void VerifySingleChangeWhenOptionChangeInUI(AbstractOptionPage page, AbstractAutomationObject automationObject, Action changeUIControl, string optionForAssertMessage)
        {
            var automationValuesBeforeChange = GetAutomationDictionary(automationObject);

            changeUIControl();

            page.SaveSettingsToStorage();

            var automationValuesAfterChange = GetAutomationDictionary(automationObject);

            Assert.Equal(automationValuesBeforeChange.Count, automationValuesAfterChange.Count);
            AssertExactlyOneChange(automationValuesBeforeChange, automationValuesAfterChange, optionForAssertMessage);
        }

        private static ImmutableDictionary<string, object> GetAutomationDictionary(AbstractAutomationObject automationObject)
        {
            var automationObjectValues = ImmutableDictionary.CreateBuilder<string, object>();
            foreach (var property in automationObject.GetType().GetProperties())
            {
                automationObjectValues.Add(property.Name, property.GetValue(automationObject, null));
            }

            return automationObjectValues.ToImmutable();
        }

        private static void AssertExactlyOneChange(ImmutableDictionary<string, object> dictionary1, ImmutableDictionary<string, object> dictionary2, string optionForAssertMessage)
        {
            var seenChange = false;
            string changedKey = null!;
            foreach (var key in dictionary1.Keys)
            {
                if (!dictionary1[key].Equals(dictionary2[key]))
                {
                    if (seenChange)
                    {
                        if (key != "ShowSnippets" && changedKey != "ShowSnippets" &&
                            key != "EnterKeyBehavior" && changedKey != "EnterKeyBehavior")
                        {
                            Assert.False(true, $"Two values ('{key}' and '{changedKey}') have changed in automation object after changing '{optionForAssertMessage}'!");
                        }
                    }

                    changedKey = key;
                    seenChange = true;
                }
            }

            Assert.True(seenChange, $"No change was found in automation object after changing '{optionForAssertMessage}'.");
        }
    }
}
