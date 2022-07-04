// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.ColorSchemes;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.CSharp.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.UnitTests;
using Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.Options
{
    [UseExportProvider]
    public class AutomationObjectTests
    {
        [Fact]
        public void TestEnsureProperStorageLocation()
        {
            using var workspace = TestWorkspace.CreateCSharp("");
            workspace.GlobalOptions.OptionChanged += (sender, e) =>
            {
                if (e.Option.Name is "RefactoringVerification" or "RenameTracking")
                {
                    return;
                }

                Assert.True(e.Option.StorageLocations.Any(s => s is RoamingProfileStorageLocation or LocalUserProfileStorageLocation or FeatureFlagStorageLocation), $"Option '{e.Option.Name}' doesn't have proper storage location for persistence.");
            };

            var automationObject = new AutomationObject(workspace);
            var automationObjectType = typeof(AutomationObject);
            foreach (var property in automationObjectType.GetProperties())
            {
                Assert.True(property.CanRead, $"'{property.Name}' must have a getter.");
                Assert.True(property.CanWrite, $"'{property.Name}' must have a setter.");

                if (property.Name is "WarnOnBuildErrors" or
                    "ShowKeywords" or
                    "ClosedFileDiagnostics" or
                    "CSharpClosedFileDiagnostics" or
                    "ShowSnippets" or
                    "Style_UseVarWhenDeclaringLocals")
                {
                    // These are deprecated and setting them have no effect.
                    continue;
                }

                var defaultValue = property.GetValue(automationObject, null);
                var newValue = GetNewValue(defaultValue, property.Name);
                Assert.True(newValue != defaultValue, $"'{property.Name}' must have a different newValue.");
                Assert.Raises<OptionChangedEventArgs>(h => workspace.GlobalOptions.OptionChanged += h, h => workspace.GlobalOptions.OptionChanged -= h, () => property.SetValue(automationObject, newValue));

                var retrievedNewValue = property.GetValue(automationObject, null);
                Assert.True(retrievedNewValue.Equals(newValue), $"'{property.Name}' didn't retrieve the new value correctly.");
            }
        }

        /// <summary>
        /// We test automation object by reading every property, then
        /// set it using a new value, then read it again and observe the new value is read.
        /// This method gets a new value for a property, given the property name and the current value.
        /// </summary>
        private static object GetNewValue(object value, string propertyName)
        {
            // The whole purpose of this method is to get a valid *new* value for the given property.
            // The general approach taken here is as follows:
            // 1. If the current value is an integer, then we use 0 for the new value, except when the old value is 0 itself, we use 1.
            // 2. If we have a string, then the common case is the property encoding a code-style option as xml, in which case we have a DiagnosticSeverity XML attribute that we can change

            // There are exceptions to the above, those are special cased here.
            // If a new option is added and failed the `TestEnsureProperStorageLocation` because it cannot get a good new value, this method can be modified accordingly to get a new value for that option.

            // For Style_RemoveUnnecessarySuppressionExclusions, it's not a CodeStyleOption<T>, so it's encoded as a regular string.
            if (propertyName == "Style_RemoveUnnecessarySuppressionExclusions")
            {
                Assert.IsType<string>(value);
                return value is "" ? "all" : "";
            }

            // For Style_NamingPreferences, it's encoded in a special XML format. We use the below string as our new value.
            if (propertyName == "Style_NamingPreferences")
            {
                Assert.IsType<string>(value);
                return """
                    <NamingPreferencesInfo SerializationVersion="5">
                      <SymbolSpecifications>
                        <SymbolSpecification ID="5c545a62-b14d-460a-88d8-e936c0a39316" Name="Class">
                          <ApplicableSymbolKindList>
                            <TypeKind>Class</TypeKind>
                          </ApplicableSymbolKindList>
                          <ApplicableAccessibilityList>
                            <AccessibilityKind>Public</AccessibilityKind>
                          </ApplicableAccessibilityList>
                          <RequiredModifierList />
                        </SymbolSpecification>
                      </SymbolSpecifications>
                      <NamingStyles>
                        <NamingStyle ID="87e7c501-9948-4b53-b1eb-a6cbe918feee" Name="Pascal Case" Prefix="" Suffix="" WordSeparator="" CapitalizationScheme="PascalCase" />
                      </NamingStyles>
                      <NamingRules>
                        <SerializableNamingRule SymbolSpecificationID="5c545a62-b14d-460a-88d8-e936c0a39316" NamingStyleID="87e7c501-9948-4b53-b1eb-a6cbe918feee" EnforcementLevel="Info" />
                      </NamingRules>
                    </NamingPreferencesInfo>
                    """;
            }

            if (value is int i)
            {
                return i == 0 ? 1 : 0;
            }
            else if (value is string s)
            {
                var xelement = XElement.Parse(s);
                if (xelement.Attribute("DiagnosticSeverity").ToString() == "Error")
                {
                    xelement.SetAttributeValue("DiagnosticSeverity", "Hidden");
                }
                else
                {
                    xelement.SetAttributeValue("DiagnosticSeverity", "Error");
                }

                return xelement.ToString();
            }

            throw ExceptionUtilities.Unreachable;
        }

        [WpfFact]
        public void TestOptionsInUIShouldBeInAutomationObject()
        {
            using var workspace = TestWorkspace.CreateCSharp("");
            var optionStore = new OptionStore(workspace.Options, Enumerable.Empty<IOption>());
            var optionService = workspace.Services.GetRequiredService<ILegacyWorkspaceOptionService>();
            var automationObject = new AutomationObject(workspace);
#pragma warning disable CS0618 // Type or member is obsolete
            // TODO: OptionPreviewControl, GridOptionPreviewControl, NamingStyleOptionPageControl.
            var pageControls = new AbstractOptionPageControl[] { new AdvancedOptionPageControl(optionStore), new IntelliSenseOptionPageControl(optionStore), new FormattingOptionPageControl(optionStore) };
#pragma warning restore CS0618 // Type or member is obsolete
            foreach (var pageControl in pageControls)
            {
                var radioButtonGroups = new Dictionary<string, List<RadioButton>>();
                foreach (var bindingExpression in pageControl.BindingExpressions)
                {
                    var target = bindingExpression.Target;
                    var optionForAssertMessage = ((FrameworkElement)target).Name;

                    var automationValuesBeforeChange = GetAutomationDictionary(automationObject);

                    if (target is CheckBox checkBox)
                    {
                        VerifySingleChangeWhenOptionChangeInUI(automationObject, () => checkBox.IsChecked = !checkBox.IsChecked, optionService, optionStore, optionForAssertMessage);
                    }
                    else if (target is ComboBox comboBox)
                    {
                        VerifySingleChangeWhenOptionChangeInUI(automationObject, () => comboBox.SelectedIndex = comboBox.SelectedIndex == 0 ? 1 : 0, optionService, optionStore, optionForAssertMessage);
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

                        continue;
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
                        VerifySingleChangeWhenOptionChangeInUI(automationObject, () => radioButton.IsChecked = true, optionService, optionStore, optionForAssertMessage: radioButton.Name);
                    }

                    // TODO: Consider asserting a non-null selectedRadioButton if https://github.com/dotnet/roslyn/issues/62363 is fixed.

                    if (selectedRadioButton is not null)
                    {
                        // Now that we tested other radio buttons in the group, the initially selected radio button is now not selected.
                        Assert.False(selectedRadioButton.IsChecked);
                        VerifySingleChangeWhenOptionChangeInUI(automationObject, () => selectedRadioButton.IsChecked = true, optionService, optionStore, optionForAssertMessage: selectedRadioButton.Name);
                    }
                }
            }

            // Above checks that all options are in AutomationObjects.
            // TODO: check that all automation object members are in options.
        }

        private static void VerifySingleChangeWhenOptionChangeInUI(AutomationObject automationObject, Action changeUIControl, ILegacyWorkspaceOptionService optionService, OptionStore optionStore, string optionForAssertMessage)
        {
            var automationValuesBeforeChange = GetAutomationDictionary(automationObject);

            changeUIControl();

            // Following simulates the SaveSettingsToStorage call.
            // Save the changes that were accumulated in the option store.
            var oldOptions = new SolutionOptionSet(optionService);
            var newOptions = (SolutionOptionSet)optionStore.GetOptions();

            // Must log the option change before setting the new option values via s_optionService,
            // otherwise oldOptions and newOptions would be identical and nothing will be logged.
            OptionLogger.Log(oldOptions, newOptions);
            optionService.SetOptions(newOptions, newOptions.GetChangedOptions());

            var automationValuesAfterChange = GetAutomationDictionary(automationObject);

            Assert.Equal(automationValuesBeforeChange.Count, automationValuesAfterChange.Count);
            AssertExactlyOneChange(automationValuesBeforeChange, automationValuesAfterChange, optionForAssertMessage);
        }

        private static ImmutableDictionary<string, object> GetAutomationDictionary(AutomationObject automationObject)
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
                        if (key != "ShowSnippets" && changedKey != "ShowSnippets" && // TODO: Remove this condition and always assert if we can remove the obsolete properties in automation object.
                            key != "EnterKeyBehavior" && changedKey != "EnterKeyBehavior" // TODO: EnterKeyBehavior is a real duplicate of InsertNewlineOnEnterWithWholeWord
                            )
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
