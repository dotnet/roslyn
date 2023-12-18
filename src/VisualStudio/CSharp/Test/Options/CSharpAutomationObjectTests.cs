// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.ColorSchemes;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.CSharp.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.UnitTests;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.Options
{
    [UseExportProvider]
    public class CSharpAutomationObjectTests : AbstractAutomationObjectTests
    {
        protected override ImmutableArray<string> SkippedOptionsForOptionChangedTest { get; } = ImmutableArray.Create(
            "WarnOnBuildErrors", "ShowKeywords", "ClosedFileDiagnostics", "CSharpClosedFileDiagnostics", "ShowSnippets", "Style_UseVarWhenDeclaringLocals");

        protected override ImmutableArray<string> SkippedOptionsForProperStorageTest { get; } = ImmutableArray.Create(
            "RefactoringVerification", "RenameTracking");

        protected override AbstractAutomationObject CreateAutomationObject(TestWorkspace workspace) => new AutomationObject(workspace);

        protected override TestWorkspace CreateWorkspace(TestComposition? composition = null) => TestWorkspace.CreateCSharp("", composition: composition);

        /// <summary>
        /// We test automation object by reading every property, then
        /// set it using a new value, then read it again and observe the new value is read.
        /// This method gets a new value for a property, given the property name and the current value.
        /// </summary>
        protected override object GetNewValue(object value, string propertyName)
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

        protected override IEnumerable<AbstractOptionPageControl> CreatePageControls(OptionStore optionStore, TestWorkspace workspace)
        {
            var threadingContext = workspace.ExportProvider.GetExportedValue<IThreadingContext>();
            var colorSchemeApplier = workspace.ExportProvider.GetExportedValue<IColorSchemeApplier>();
            yield return new AdvancedOptionPageControl(optionStore, threadingContext, colorSchemeApplier);
            yield return new IntelliSenseOptionPageControl(optionStore);
            yield return new FormattingOptionPageControl(optionStore);
            // TODO: OptionPreviewControl, GridOptionPreviewControl, NamingStyleOptionPageControl.
        }
    }
}
