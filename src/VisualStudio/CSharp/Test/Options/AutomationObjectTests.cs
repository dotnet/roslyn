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

        private static object GetNewValue(object value, string propertyName)
        {
            if (propertyName == "Style_RemoveUnnecessarySuppressionExclusions")
            {
                Assert.IsType<string>(value);
                return value is "" ? "all" : "";
            }

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

        [Fact]
        public void TestOptionsInUIShouldBeInAutomationObject()
        {
            using var workspace = TestWorkspace.CreateCSharp("");
            var optionStore = new OptionStore(workspace.Options, Enumerable.Empty<IOption>());

            var advancedOptions = new AdvancedOptionPage();
            var serviceProvider = workspace.ExportProvider.GetExportedValue<MockServiceProvider>();
            var advancedOptionsPage = advancedOptions.CreateOptionPageForTests(serviceProvider, optionStore);
            foreach (var bindingExpression in advancedOptionsPage.BindingExpressions)
            {
                var target = bindingExpression.Target;
                var optionForAssertMessage = ((FrameworkElement)target).Name;

                var automationValuesBeforeChange = GetAutomationDictionary((AutomationObject)advancedOptions.AutomationObject);

                if (target is CheckBox checkBox)
                {
                    checkBox.IsChecked = !checkBox.IsChecked;
                }
                else if (target is ComboBox comboBox)
                {
                    comboBox.SelectedIndex = comboBox.SelectedIndex == 0 ? 1 : 0;
                }
                else if (target is RadioButton)
                {
                    // skip for now. TODO later..
                    continue;
                }

                advancedOptions.SaveSettingsToStorage();

                var automationValuesAfterChange = GetAutomationDictionary((AutomationObject)advancedOptions.AutomationObject);

                Assert.Equal(automationValuesBeforeChange.Count, automationValuesAfterChange.Count);
                AssertExactlyOneChange(automationValuesBeforeChange, automationValuesAfterChange, optionForAssertMessage);
            }

            _ = new IntelliSenseOptionPageControl(optionStore);
            _ = new FormattingOptionPageControl(optionStore);

            // Above checks that all options are in AutomationObjects.
            // TODO: check that all automation object members are in options.
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
                        Assert.False(true, $"Two values ('{key}' and '{changedKey}') have changed in automation object after changing '{optionForAssertMessage}'!");
                    }

                    changedKey = key;
                    seenChange = true;
                }
            }

            Assert.True(seenChange, $"No change was found in automation object after changing '{optionForAssertMessage}'.");
        }
    }
}
