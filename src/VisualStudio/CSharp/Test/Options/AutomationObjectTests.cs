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

        [StaFact]
        public void TestOptionsInUIShouldBeInAutomationObject()
        {

            using var workspace = TestWorkspace.CreateCSharp("");
            var optionStore = new OptionStore(workspace.Options, Enumerable.Empty<IOption>());
            var optionService = workspace.Services.GetRequiredService<ILegacyWorkspaceOptionService>();
            var automationObject = new AutomationObject(workspace);
            var pageControls = new AbstractOptionPageControl[] { new AdvancedOptionPageControl(optionStore), new IntelliSenseOptionPageControl(optionStore), new FormattingOptionPageControl(optionStore) };
            foreach (var pageControl in pageControls)
            {

                foreach (var bindingExpression in pageControl.BindingExpressions)
                {
                    var target = bindingExpression.Target;
                    var optionForAssertMessage = ((FrameworkElement)target).Name;
                    if (optionForAssertMessage is
                        // Advanced page
                        "Run_background_code_analysis_for" or
                        "Show_compiler_errors_and_warnings_for" or
                        "DisplayDiagnosticsInline" or
                        "Run_code_analysis_in_separate_process" or
                        "Enable_file_logging_for_diagnostics" or
                        "Rename_asynchronously_exerimental" or
                        "ComputeQuickActionsAsynchronouslyExperimental" or
                        "Show_outlining_for_declaration_level_constructs" or
                        "Show_outlining_for_code_level_constructs" or
                        "Show_outlining_for_comments_and_preprocessor_regions" or
                        "Collapse_regions_when_collapsing_to_definitions" or
                        "Show_guides_for_declaration_level_constructs" or
                        "Show_guides_for_code_level_constructs" or
                        "InsertSlashSlashAtTheStartOfNewLinesWhenWritingSingleLineComments" or
                        "ShowRemarksInQuickInfo" or
                        "Split_string_literals_on_enter" or
                        "Report_invalid_placeholders_in_string_dot_format_calls" or
                        "Underline_reassigned_variables" or
                        "Enable_all_features_in_opened_files_from_source_generators" or
                        "Colorize_regular_expressions" or
                        "Report_invalid_regular_expressions" or
                        "Highlight_related_regular_expression_components_under_cursor" or
                        "Show_completion_list" or
                        "Colorize_JSON_strings" or
                        "Report_invalid_JSON_strings" or
                        "Highlight_related_JSON_components_under_cursor" or
                        "Editor_color_scheme" or
                        "DisplayAllHintsWhilePressingAltF1" or
                        "ColorHints" or
                        "DisplayInlineParameterNameHints" or
                        "ShowHintsForLiterals" or
                        "ShowHintsForNewExpressions" or
                        "ShowHintsForEverythingElse" or
                        "ShowHintsForIndexers" or
                        "SuppressHintsWhenParameterNameMatchesTheMethodsIntent" or
                        "SuppressHintsWhenParameterNamesDifferOnlyBySuffix" or
                        "SuppressHintsWhenParameterNamesMatchArgumentNames" or
                        "DisplayInlineTypeHints" or
                        "ShowHintsForVariablesWithInferredTypes" or
                        "ShowHintsForLambdaParameterTypes" or
                        "ShowHintsForImplicitObjectCreation" or
                        "ShowInheritanceMargin" or
                        "InheritanceMarginCombinedWithIndicatorMargin" or
                        "IncludeGlobalImports" or
                        "AutomaticallyOpenStackTraceExplorer" or
                        // IntelliSense page
                        "Show_name_suggestions" or
                        // formatting page
                        "FormatOnReturnCheckBox")
                    {
                        // The above options are not persisted via automation object. These should be fixed.
                        continue;
                    }

                    var automationValuesBeforeChange = GetAutomationDictionary(automationObject);

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

                    //advancedOptions.SaveSettingsToStorage();
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
            }

            // Above checks that all options are in AutomationObjects.
            // TODO: check that all automation object members are in options.

            // The above verifies options passed to BindToOption.
            // TODO: AbstractOptionPreviewViewModel
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
