// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Options.UnitTests;

[UseExportProvider]
public sealed class SolutionAnalyzerConfigOptionsUpdaterTests
{
    private static TestWorkspace CreateWorkspace()
    {
        var workspace = new LspTestWorkspace(LspTestCompositions.LanguageServerProtocol
            .RemoveParts(typeof(MockFallbackAnalyzerConfigOptionsProvider))
            .ExportProviderFactory
            .CreateExportProvider());

        var updater = (SolutionAnalyzerConfigOptionsUpdater)workspace.ExportProvider.GetExports<IEventListener>().Single(e => e.Value is SolutionAnalyzerConfigOptionsUpdater).Value;
        var listenerProvider = workspace.GetService<MockWorkspaceEventListenerProvider>();
        listenerProvider.EventListeners = [updater];

        return workspace;
    }

    [Fact]
    public void FlowsGlobalOptionsToWorkspace()
    {
        using var workspace = CreateWorkspace();

        var globalOptions = workspace.GetService<IGlobalOptionService>();

        // default values:
        Assert.False(globalOptions.GetOption(FormattingOptions2.InsertFinalNewLine));
        Assert.Equal(4, globalOptions.GetOption(FormattingOptions2.IndentationSize, LanguageNames.CSharp));
        Assert.Equal(4, globalOptions.GetOption(FormattingOptions2.IndentationSize, LanguageNames.VisualBasic));

        // C# project hasn't been loaded to the workspace yet:
        Assert.Empty(workspace.CurrentSolution.FallbackAnalyzerOptions);

        var project = new TestHostProject(workspace, "proj1", LanguageNames.CSharp);
        workspace.AddTestProject(project);

        AssertOptionValue(FormattingOptions2.InsertFinalNewLine, LanguageNames.CSharp, "false");
        AssertOptionValue(FormattingOptions2.IndentationSize, LanguageNames.CSharp, "4");

        globalOptions.SetGlobalOptions(
        [
            new KeyValuePair<OptionKey2, object?>(FormattingOptions2.InsertFinalNewLine, true),
            new KeyValuePair<OptionKey2, object?>(new OptionKey2(FormattingOptions2.IndentationSize, LanguageNames.CSharp), 3),
            new KeyValuePair<OptionKey2, object?>(new OptionKey2(FormattingOptions2.IndentationSize, LanguageNames.VisualBasic), 5)
        ]);

        // editorconfig option set as a global option should flow to the solution snapshot:
        AssertOptionValue(FormattingOptions2.InsertFinalNewLine, LanguageNames.CSharp, "true");
        AssertOptionValue(FormattingOptions2.IndentationSize, LanguageNames.CSharp, "3");

        workspace.OnProjectRemoved(project.Id);

        // last C# project removed -> fallback options removed:
        Assert.Empty(workspace.CurrentSolution.FallbackAnalyzerOptions);

        workspace.AddTestProject(new TestHostProject(workspace, "proj2", LanguageNames.VisualBasic));

        AssertOptionValue(FormattingOptions2.InsertFinalNewLine, LanguageNames.VisualBasic, "true");
        AssertOptionValue(FormattingOptions2.IndentationSize, LanguageNames.VisualBasic, "5");

        Assert.False(workspace.CurrentSolution.FallbackAnalyzerOptions.TryGetValue(LanguageNames.CSharp, out _));

        // VB and C# projects added:

        workspace.AddTestProject(new TestHostProject(workspace, "proj3", LanguageNames.CSharp));

        AssertOptionValue(FormattingOptions2.InsertFinalNewLine, LanguageNames.VisualBasic, "true");
        AssertOptionValue(FormattingOptions2.InsertFinalNewLine, LanguageNames.CSharp, "true");
        AssertOptionValue(FormattingOptions2.IndentationSize, LanguageNames.VisualBasic, "5");
        AssertOptionValue(FormattingOptions2.IndentationSize, LanguageNames.CSharp, "3");

        globalOptions.SetGlobalOption(FormattingOptions2.InsertFinalNewLine, false);

        AssertOptionValue(FormattingOptions2.InsertFinalNewLine, LanguageNames.VisualBasic, "false");
        AssertOptionValue(FormattingOptions2.InsertFinalNewLine, LanguageNames.CSharp, "false");
        AssertOptionValue(FormattingOptions2.IndentationSize, LanguageNames.VisualBasic, "5");
        AssertOptionValue(FormattingOptions2.IndentationSize, LanguageNames.CSharp, "3");

        void AssertOptionValue(IOption2 option, string language, string expectedValue)
        {
            Assert.True(workspace.CurrentSolution.FallbackAnalyzerOptions.TryGetValue(language, out var fallbackOptions));
            Assert.True(fallbackOptions!.TryGetValue(option.Definition.ConfigName, out var configValue));
            Assert.Equal(expectedValue, configValue);
        }
    }

    [Fact]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2297536")]
    public void FlowsNamingStylePreferencesToWorkspace()
    {
        using var workspace = CreateWorkspace();

        Assert.Empty(workspace.CurrentSolution.Projects);
        var globalOptions = workspace.GetService<IGlobalOptionService>();

        var initialPeferences = OptionsTestHelpers.CreateNamingStylePreferences(
            ([SymbolKind.Property], Capitalization.AllUpper, ReportDiagnostic.Error));

        globalOptions.SetGlobalOption(NamingStyleOptions.NamingPreferences, LanguageNames.CSharp, initialPeferences);

        var testProjectWithoutConfig = new TestHostProject(workspace, "proj_without_config", LanguageNames.CSharp);

        testProjectWithoutConfig.AddDocument(new TestHostDocument("""
            class MyClass1;
            """,
            filePath: Path.Combine(TempRoot.Root, "proj_without_config", "test.cs")));

        var testProjectWithConfig = new TestHostProject(workspace, "proj_with_config", LanguageNames.CSharp);

        // explicitly specified style should override style specified in the fallback:
        testProjectWithConfig.AddAnalyzerConfigDocument(new TestHostDocument(
            """
            [*.cs]
            dotnet_naming_rule.rule1.severity = warning
            dotnet_naming_rule.rule1.symbols = symbols1
            dotnet_naming_rule.rule1.style = style1

            dotnet_naming_symbols.symbols1.applicable_kinds = class
            dotnet_naming_symbols.symbols1.applicable_accessibilities = *
            dotnet_naming_style.style1.capitalization = camel_case
            """,
            filePath: Path.Combine(TempRoot.Root, "proj_with_config", ".editorconfig")));

        testProjectWithConfig.AddDocument(new TestHostDocument("""
            class MyClass2;
            """,
            filePath: Path.Combine(TempRoot.Root, "proj_with_config", "test.cs")));

        // No fallback options before a project is added.
        Assert.False(workspace.CurrentSolution.FallbackAnalyzerOptions.TryGetValue(LanguageNames.CSharp, out _));

        workspace.AddTestProject(testProjectWithoutConfig);
        workspace.AddTestProject(testProjectWithConfig);

        // Once a C# project is added the preferences stored in global options should be applied to fallback options:
        Assert.True(workspace.CurrentSolution.FallbackAnalyzerOptions.TryGetValue(LanguageNames.CSharp, out var fallbackOptions));
        AssertEx.SequenceEqual(
            initialPeferences.Rules.NamingRules.Select(r => r.Inspect()),
            fallbackOptions.GetNamingStylePreferences().Rules.NamingRules.Select(r => r.Inspect()));

        var hostPeferences = OptionsTestHelpers.CreateNamingStylePreferences(
            ([MethodKind.Ordinary], Capitalization.PascalCase, ReportDiagnostic.Error),
            ([MethodKind.Ordinary, SymbolKind.Field], Capitalization.PascalCase, ReportDiagnostic.Error));

        globalOptions.SetGlobalOption(NamingStyleOptions.NamingPreferences, LanguageNames.CSharp, hostPeferences);

        // Initial preferences should be replaced by host preferences.
        // Note: rules are ordered but symbol and naming style specifications are not.
        Assert.True(workspace.CurrentSolution.FallbackAnalyzerOptions.TryGetValue(LanguageNames.CSharp, out fallbackOptions));
        AssertEx.SequenceEqual(
            hostPeferences.Rules.NamingRules.Select(r => r.Inspect()),
            fallbackOptions.GetNamingStylePreferences().Rules.NamingRules.Select(r => r.Inspect()));

        var projectWithConfig = workspace.CurrentSolution.GetRequiredProject(testProjectWithConfig.Id);
        var treeWithConfig = projectWithConfig.Documents.Single().GetSyntaxTreeSynchronously(CancellationToken.None);
        Assert.NotNull(treeWithConfig);
        var documentOptions = projectWithConfig.HostAnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(treeWithConfig);

        Assert.True(documentOptions.TryGetEditorConfigOption<NamingStylePreferences>(NamingStyleOptions.NamingPreferences, out var documentPreferences));
        Assert.NotNull(documentPreferences);

        // Only naming styles specified in the editorconfig are present.
        // Host preferences are ignored. This behavior is consistent with VS 16.11.
        AssertEx.EqualOrDiff("""
            <NamingPreferencesInfo SerializationVersion="5">
              <SymbolSpecifications>
                <SymbolSpecification ID="0" Name="symbols1">
                  <ApplicableSymbolKindList>
                    <TypeKind>Class</TypeKind>
                  </ApplicableSymbolKindList>
                  <ApplicableAccessibilityList>
                    <AccessibilityKind>NotApplicable</AccessibilityKind>
                    <AccessibilityKind>Public</AccessibilityKind>
                    <AccessibilityKind>Internal</AccessibilityKind>
                    <AccessibilityKind>Private</AccessibilityKind>
                    <AccessibilityKind>Protected</AccessibilityKind>
                    <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
                    <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
                  </ApplicableAccessibilityList>
                  <RequiredModifierList />
                </SymbolSpecification>
              </SymbolSpecifications>
              <NamingStyles>
                <NamingStyle ID="1" Name="style1" Prefix="" Suffix="" WordSeparator="" CapitalizationScheme="CamelCase" />
              </NamingStyles>
              <NamingRules>
                <SerializableNamingRule SymbolSpecificationID="0" NamingStyleID="1" EnforcementLevel="Warning" />
              </NamingRules>
            </NamingPreferencesInfo>
            """,
            documentPreferences.Inspect());

        var projectWithoutConfig = workspace.CurrentSolution.GetRequiredProject(testProjectWithoutConfig.Id);
        var treeWithoutConfig = projectWithoutConfig.Documents.Single().GetSyntaxTreeSynchronously(CancellationToken.None);
        Assert.NotNull(treeWithoutConfig);
        documentOptions = projectWithoutConfig.HostAnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(treeWithoutConfig);

        Assert.True(documentOptions.TryGetEditorConfigOption(NamingStyleOptions.NamingPreferences, out documentPreferences));
        Assert.NotNull(documentPreferences);

        // Host preferences:
        AssertEx.EqualOrDiff("""
            <NamingPreferencesInfo SerializationVersion="5">
              <SymbolSpecifications>
                <SymbolSpecification ID="0" Name="symbols0">
                  <ApplicableSymbolKindList>
                    <MethodKind>Ordinary</MethodKind>
                  </ApplicableSymbolKindList>
                  <ApplicableAccessibilityList>
                    <AccessibilityKind>NotApplicable</AccessibilityKind>
                    <AccessibilityKind>Public</AccessibilityKind>
                    <AccessibilityKind>Internal</AccessibilityKind>
                    <AccessibilityKind>Private</AccessibilityKind>
                    <AccessibilityKind>Protected</AccessibilityKind>
                    <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
                    <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
                  </ApplicableAccessibilityList>
                  <RequiredModifierList />
                </SymbolSpecification>
                <SymbolSpecification ID="1" Name="symbols1">
                  <ApplicableSymbolKindList>
                    <MethodKind>Ordinary</MethodKind>
                    <SymbolKind>Field</SymbolKind>
                  </ApplicableSymbolKindList>
                  <ApplicableAccessibilityList>
                    <AccessibilityKind>NotApplicable</AccessibilityKind>
                    <AccessibilityKind>Public</AccessibilityKind>
                    <AccessibilityKind>Internal</AccessibilityKind>
                    <AccessibilityKind>Private</AccessibilityKind>
                    <AccessibilityKind>Protected</AccessibilityKind>
                    <AccessibilityKind>ProtectedAndInternal</AccessibilityKind>
                    <AccessibilityKind>ProtectedOrInternal</AccessibilityKind>
                  </ApplicableAccessibilityList>
                  <RequiredModifierList />
                </SymbolSpecification>
              </SymbolSpecifications>
              <NamingStyles>
                <NamingStyle ID="2" Name="style0" Prefix="" Suffix="" WordSeparator="" CapitalizationScheme="PascalCase" />
                <NamingStyle ID="3" Name="style1" Prefix="" Suffix="" WordSeparator="" CapitalizationScheme="PascalCase" />
              </NamingStyles>
              <NamingRules>
                <SerializableNamingRule SymbolSpecificationID="0" NamingStyleID="2" EnforcementLevel="Error" />
                <SerializableNamingRule SymbolSpecificationID="1" NamingStyleID="3" EnforcementLevel="Error" />
              </NamingRules>
            </NamingPreferencesInfo>
            """,
            documentPreferences.Inspect());
    }

    [Fact]
    public void IgnoresNonEditorConfigOptions()
    {
        using var workspace = CreateWorkspace();

        var globalOptions = workspace.GetService<IGlobalOptionService>();

        var option = new Option2<bool>("test_option", defaultValue: false, isEditorConfigOption: false);

        Assert.False(globalOptions.GetOption(option));
        Assert.Empty(workspace.CurrentSolution.FallbackAnalyzerOptions);

        var project = new TestHostProject(workspace, "proj1", LanguageNames.CSharp);
        workspace.AddTestProject(project);

        var optionsAfterProjectAdded = workspace.CurrentSolution.FallbackAnalyzerOptions;

        Assert.NotEmpty(optionsAfterProjectAdded);
        Assert.False(optionsAfterProjectAdded.ContainsKey("test_option"));

        globalOptions.SetGlobalOption(option, true);

        Assert.Same(optionsAfterProjectAdded, workspace.CurrentSolution.FallbackAnalyzerOptions);
    }
}
