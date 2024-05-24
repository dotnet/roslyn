// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.EditorConfig;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
public partial class SettingsUpdaterTests : TestBase
{
    private const string EditorconfigPath = "/a/b/config";

    private static Workspace CreateWorkspaceWithProjectAndDocuments()
    {
        var projectId = ProjectId.CreateNewId();

        var workspace = new AdhocWorkspace(EditorTestCompositions.EditorFeatures.GetHostServices(), WorkspaceKind.Host);

        Assert.True(workspace.TryApplyChanges(workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Create(), "proj1", "proj1.dll", LanguageNames.CSharp, filePath: "/a/b/proj1.csproj"))
            .AddDocument(DocumentId.CreateNewId(projectId), "goo.cs", "public class Goo { }")
            .AddAdditionalDocument(DocumentId.CreateNewId(projectId), "add.txt", "text")
            .AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId), "editorcfg", SourceText.From(""), filePath: EditorconfigPath)));

        return workspace;
    }

    private static IGlobalOptionService GetGlobalOptions(Workspace workspace)
        => workspace.Services.SolutionServices.ExportProvider.GetExportedValue<IGlobalOptionService>();

    private static AnalyzerConfigDocument CreateAnalyzerConfigDocument(Workspace workspace, string contents)
    {
        var solution = workspace.CurrentSolution;
        var documentId = solution.Projects.Single().State.AnalyzerConfigDocumentStates.Ids.First();
        var text = SourceText.From(contents);
        var newSolution1 = solution.WithAnalyzerConfigDocumentText(documentId, text, PreservationMode.PreserveIdentity);
        var analyzerConfigDocument = newSolution1.GetAnalyzerConfigDocument(documentId);
        Assert.True(analyzerConfigDocument!.TryGetText(out var actualText));
        Assert.Same(text, actualText);
        return analyzerConfigDocument;
    }

    private static async Task TestAsync(string initialEditorConfig, string updatedEditorConfig, params (IOption2, object)[] options)
    {
        using var workspace = CreateWorkspaceWithProjectAndDocuments();
        var analyzerConfigDocument = CreateAnalyzerConfigDocument(workspace, initialEditorConfig);
        var sourcetext = await analyzerConfigDocument.GetTextAsync(default);
        var result = SettingsUpdateHelper.TryUpdateAnalyzerConfigDocument(sourcetext, analyzerConfigDocument.FilePath!, options);
        Assert.Equal(updatedEditorConfig, result?.ToString());
    }

    private static async Task TestAsync(string initialEditorConfig, string updatedEditorConfig, params (AnalyzerSetting, ReportDiagnostic)[] options)
    {
        using var workspace = CreateWorkspaceWithProjectAndDocuments();
        var analyzerConfigDocument = CreateAnalyzerConfigDocument(workspace, initialEditorConfig);
        var sourcetext = await analyzerConfigDocument.GetTextAsync(default);
        var result = SettingsUpdateHelper.TryUpdateAnalyzerConfigDocument(sourcetext, analyzerConfigDocument.FilePath!, options);
        Assert.Equal(updatedEditorConfig, result?.ToString());
    }

    [Fact]
    public async Task TestAddNewWhitespaceOptionAsync()
    {
        await TestAsync(
            string.Empty,
            "[*.cs]\r\ncsharp_new_line_before_else = true",
            (CSharpFormattingOptions2.NewLineForElse, true));
    }

    [Fact]
    public async Task TestAddNewBoolCodeStyleOptionWithSeverityAsync()
    {
        await TestAsync(
            string.Empty,
            "[*.cs]\r\ncsharp_style_throw_expression = true:suggestion",
            (CSharpCodeStyleOptions.PreferThrowExpression, CodeStyleOption2.TrueWithSuggestionEnforcement));
    }

    [Fact]
    public async Task TestAddNewEnumCodeStyleOptionWithSeverityAsync()
    {
        var option = new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.InsideNamespace, NotificationOption2.Warning);
        await TestAsync(
            string.Empty,
            "[*.cs]\r\ncsharp_using_directive_placement = inside_namespace:warning",
            (CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, option));
    }

    [Theory, CombinatorialData]
    internal async Task TestAddNewAnalyzerOptionOptionAsync(
        [CombinatorialValues(Language.CSharp, Language.VisualBasic, (Language.CSharp | Language.VisualBasic))]
        Language language,
        [CombinatorialValues(ReportDiagnostic.Warn, ReportDiagnostic.Error, ReportDiagnostic.Info, ReportDiagnostic.Hidden, ReportDiagnostic.Suppress)]
        ReportDiagnostic severity)
    {
        var expectedHeader = "";
        if (language.HasFlag(Language.CSharp) && language.HasFlag(Language.VisualBasic))
        {
            expectedHeader = "[*.{cs,vb}]";
        }
        else if (language.HasFlag(Language.CSharp))
        {
            expectedHeader = "[*.cs]";
        }
        else if (language.HasFlag(Language.VisualBasic))
        {
            expectedHeader = "[*.vb]";
        }

        var expectedSeverity = severity.ToEditorConfigString();

        var id = "Test001";
        var descriptor = new DiagnosticDescriptor(id: id, title: "", messageFormat: "", category: "Naming", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: false);
        var analyzerSetting = new AnalyzerSetting(descriptor, ReportDiagnostic.Suppress, null!, language, new SettingLocation(EditorConfigSettings.LocationKind.VisualStudio, null));

        await TestAsync(
            string.Empty,
            $"{expectedHeader}\r\ndotnet_diagnostic.{id}.severity = {expectedSeverity}",
            (analyzerSetting, severity));
    }

    [Fact]
    public async Task TestUpdateExistingWhitespaceOptionAsync()
    {
        await TestAsync(
            "[*.cs]\r\ncsharp_new_line_before_else = true",
            "[*.cs]\r\ncsharp_new_line_before_else = false",
            (CSharpFormattingOptions2.NewLineForElse, false));
    }

    [Fact]
    public async Task TestAddNewWhitespaceOptionToExistingFileAsync()
    {
        var initialEditorConfig = @"
[*.{cs,vb}]

# CA1000: Do not declare static members on generic types
dotnet_diagnostic.CA1000.severity = false

";

        var updatedEditorConfig = @"
[*.{cs,vb}]

# CA1000: Do not declare static members on generic types
dotnet_diagnostic.CA1000.severity = false


[*.cs]
csharp_new_line_before_else = true";
        await TestAsync(
            initialEditorConfig,
            updatedEditorConfig,
            (CSharpFormattingOptions2.NewLineForElse, true));
    }

    [Fact]
    public async Task TestAddNewWhitespaceOptionToWithNonMathcingGroupsAsync()
    {
        var initialEditorConfig = @"
root = true

# Xml files
[*.xml]
indent_size = 2";

        var updatedEditorConfig = @"
root = true

# Xml files
[*.xml]
indent_size = 2
[*.cs]
csharp_new_line_before_else = true";
        await TestAsync(
            initialEditorConfig,
            updatedEditorConfig,
            (CSharpFormattingOptions2.NewLineForElse, true));
    }

    [Fact]
    public async Task TestAddNewWhitespaceOptionWithStarGroup()
    {
        var initialEditorConfig = @"
root = true

# Xml files
[*.xml]
indent_size = 2

# Dotnet code style settings:
[*.{cs,vb}]

# CSharp code style settings:
[*.cs]";

        var updatedEditorConfig = @"
root = true

# Xml files
[*.xml]
indent_size = 2

# Dotnet code style settings:
[*.{cs,vb}]

# CSharp code style settings:
[*.cs]
csharp_new_line_before_else = true";

        await TestAsync(
            initialEditorConfig,
            updatedEditorConfig,
            (CSharpFormattingOptions2.NewLineForElse, true));
    }

    [Fact]
    public async Task TestAddMultimpleNewWhitespaceOptions()
    {
        await TestAsync(
            string.Empty,
            "[*.cs]\r\ncsharp_new_line_before_else = true\r\ncsharp_new_line_before_catch = true\r\ncsharp_new_line_before_finally = true",
            (CSharpFormattingOptions2.NewLineForElse, true),
            (CSharpFormattingOptions2.NewLineForCatch, true),
            (CSharpFormattingOptions2.NewLineForFinally, true));
    }

    [Fact]
    public async Task TestAddOptionThatAppliesToBothLanguages()
    {
        var initialEditorConfig = @"
root = true

# Xml files
[*.xml]
indent_size = 2

# Dotnet code style settings:
[*.{cs,vb}]

# CSharp code style settings:
[*.cs]";

        var updatedEditorConfig = @"
root = true

# Xml files
[*.xml]
indent_size = 2

# Dotnet code style settings:
[*.{cs,vb}]
dotnet_sort_system_directives_first = true

# CSharp code style settings:
[*.cs]";

        await TestAsync(
            initialEditorConfig,
            updatedEditorConfig,
            (GenerationOptions.PlaceSystemNamespaceFirst, true));
    }

    [Fact]
    public async Task TestAddOptionWithRelativePathGroupingPresent()
    {
        var initialEditorConfig = @"
root = true

# Xml files
[*.xml]
indent_size = 2

# Dotnet code style settings:
[*.{cs,vb}]

# Test CSharp code style settings:
[*Test.cs]

# CSharp code style settings:
[*.cs]";

        var updatedEditorConfig = @"
root = true

# Xml files
[*.xml]
indent_size = 2

# Dotnet code style settings:
[*.{cs,vb}]

# Test CSharp code style settings:
[*Test.cs]

# CSharp code style settings:
[*.cs]
csharp_new_line_before_else = true";

        await TestAsync(
            initialEditorConfig,
            updatedEditorConfig,
            (CSharpFormattingOptions2.NewLineForElse, true));
    }

    [Fact]
    public async Task TestAnalyzerSettingsUpdaterService()
    {
        var workspace = CreateWorkspaceWithProjectAndDocuments();
        var updater = new AnalyzerSettingsUpdater(workspace, EditorconfigPath);
        var id = "Test001";
        var descriptor = new DiagnosticDescriptor(id: id, title: "", messageFormat: "", category: "Naming", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: false);
        var analyzerSetting = new AnalyzerSetting(descriptor, ReportDiagnostic.Suppress, updater, Language.CSharp, new SettingLocation(EditorConfigSettings.LocationKind.VisualStudio, null));
        analyzerSetting.ChangeSeverity(ReportDiagnostic.Error);
        var updates = await updater.GetChangedEditorConfigAsync(default);
        var update = Assert.Single(updates);
        Assert.Equal($"[*.cs]\r\ndotnet_diagnostic.{id}.severity = error", update.NewText);
    }

    [Fact]
    public async Task TestCodeStyleSettingUpdaterService()
    {
        var workspace = CreateWorkspaceWithProjectAndDocuments();
        var globalOptions = GetGlobalOptions(workspace);

        var updater = new OptionUpdater(workspace, EditorconfigPath);

        var value = "false:silent";

        var options = new TieredAnalyzerConfigOptions(
            new TestAnalyzerConfigOptions(key => value),
            globalOptions,
            LanguageNames.CSharp,
            EditorconfigPath);

        var setting = CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, "description", options, updater);
        setting.ChangeSeverity(ReportDiagnostic.Error);
        var updates = await updater.GetChangedEditorConfigAsync(default);
        var update = Assert.Single(updates);
        Assert.Equal("[*.cs]\r\ncsharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental = false:error", update.NewText);
        value = "false:error";

        var solution = workspace.CurrentSolution;
        var editorconfig = solution.Projects.SelectMany(p => p.AnalyzerConfigDocuments.Where(a => a.FilePath == EditorconfigPath)).Single();
        var text = await editorconfig.GetTextAsync();

        var newSolution = solution.WithAnalyzerConfigDocumentText(editorconfig.Id, text);
        Assert.True(workspace.TryApplyChanges(newSolution));

        setting.ChangeValue(0);
        updates = await updater.GetChangedEditorConfigAsync(default);
        update = Assert.Single(updates);
        Assert.Equal("[*.cs]\r\ncsharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental = true:error", update.NewText);
    }

    [Fact]
    public async Task TestWhitespaceSettingUpdaterService()
    {
        var workspace = CreateWorkspaceWithProjectAndDocuments();
        var globalOptions = GetGlobalOptions(workspace);
        var updater = new OptionUpdater(workspace, EditorconfigPath);

        var options = new TieredAnalyzerConfigOptions(
            TestAnalyzerConfigOptions.Instance,
            globalOptions,
            LanguageNames.CSharp,
            EditorconfigPath);

        var setting = Setting.Create(CSharpFormattingOptions2.NewLineForElse, "description", options, updater);
        setting.SetValue(false);
        var updates = await updater.GetChangedEditorConfigAsync(default);
        var update = Assert.Single(updates);
        Assert.Equal("[*.cs]\r\ncsharp_new_line_before_else = false", update.NewText);
    }

    [Fact]
    public async Task TestNamingStyleSettingsUpdater()
    {
        var workspace = CreateWorkspaceWithProjectAndDocuments();
        var settingsProviderFactory = workspace.Services.GetRequiredService<IWorkspaceSettingsProviderFactory<NamingStyleSetting>>();
        var settingsProvider = settingsProviderFactory.GetForFile(EditorconfigPath);
        var model = new TestViewModel();
        settingsProvider.RegisterViewModel(model);
        var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
        Assert.Equal(3, dataSnapShot.Length);

        var setting0 = dataSnapShot[0];
        var setting1 = dataSnapShot[1];
        var setting2 = dataSnapShot[2];

        setting0.ChangeSeverity(ReportDiagnostic.Error);

        var newText = await settingsProvider.GetChangedEditorConfigAsync(SourceText.From(string.Empty));
        var fileText = newText.ToString();
        Assert.Equal(ExpectedInitialEditorConfig, fileText);
        Assert.Equal(ReportDiagnostic.Error, setting0.Severity);

        setting1.ChangeSeverity(ReportDiagnostic.Error);
        setting2.ChangeSeverity(ReportDiagnostic.Error);

        newText = await settingsProvider.GetChangedEditorConfigAsync(newText);
        fileText = newText.ToString();
        Assert.Equal(ExpectedEditorConfigAfterAllSeveritiesChanged, fileText);
        Assert.Equal(ReportDiagnostic.Error, setting0.Severity);
        Assert.Equal(ReportDiagnostic.Error, setting0.Severity);

        var selectedStyleIndex0 = Array.IndexOf(setting0.AllStyles, setting0.StyleName);
        Assert.Equal(1, selectedStyleIndex0);

        setting0.ChangeStyle(0);
        newText = await settingsProvider.GetChangedEditorConfigAsync(newText);
        fileText = newText.ToString();
        Assert.Equal(ExpectedEditorConfigAfterSymbolSpecChange, fileText);
        Assert.Equal("pascal_case", setting0.StyleName);
    }

    private readonly string ExpectedInitialEditorConfig =
@$"
[*.{{cs,vb}}]
#### {CompilerExtensionsResources.Naming_styles} ####

# {CompilerExtensionsResources.Naming_rules}

dotnet_naming_rule.{(CompilerExtensionsResources.Interface + "_should_be_" + CompilerExtensionsResources.Begins_with_I.Replace(' ', '_')).ToLowerInvariant()}.severity = error
dotnet_naming_rule.{(CompilerExtensionsResources.Interface + "_should_be_" + CompilerExtensionsResources.Begins_with_I.Replace(' ', '_')).ToLowerInvariant()}.symbols = {CompilerExtensionsResources.Interface.ToLowerInvariant()}
dotnet_naming_rule.{(CompilerExtensionsResources.Interface + "_should_be_" + CompilerExtensionsResources.Begins_with_I.Replace(' ', '_')).ToLowerInvariant()}.style = {CompilerExtensionsResources.Begins_with_I.Replace(' ', '_').ToLowerInvariant()}

dotnet_naming_rule.{(CompilerExtensionsResources.Types + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.severity = suggestion
dotnet_naming_rule.{(CompilerExtensionsResources.Types + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.symbols = {CompilerExtensionsResources.Types.ToLowerInvariant()}
dotnet_naming_rule.{(CompilerExtensionsResources.Types + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.style = {CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}

dotnet_naming_rule.{(CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_') + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.severity = suggestion
dotnet_naming_rule.{(CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_') + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.symbols = {CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_').ToLowerInvariant()}
dotnet_naming_rule.{(CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_') + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.style = {CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}

# {CompilerExtensionsResources.Symbol_specifications}

dotnet_naming_symbols.{CompilerExtensionsResources.Interface.ToLowerInvariant()}.applicable_kinds = interface
dotnet_naming_symbols.{CompilerExtensionsResources.Interface.ToLowerInvariant()}.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
dotnet_naming_symbols.{CompilerExtensionsResources.Interface.ToLowerInvariant()}.required_modifiers = 

dotnet_naming_symbols.{CompilerExtensionsResources.Types.ToLowerInvariant()}.applicable_kinds = class, struct, interface, enum
dotnet_naming_symbols.{CompilerExtensionsResources.Types.ToLowerInvariant()}.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
dotnet_naming_symbols.{CompilerExtensionsResources.Types.ToLowerInvariant()}.required_modifiers = 

dotnet_naming_symbols.{CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_').ToLowerInvariant()}.applicable_kinds = property, event, method
dotnet_naming_symbols.{CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_').ToLowerInvariant()}.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
dotnet_naming_symbols.{CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_').ToLowerInvariant()}.required_modifiers = 

# {CompilerExtensionsResources.Naming_styles}

dotnet_naming_style.{CompilerExtensionsResources.Begins_with_I.Replace(' ', '_').ToLowerInvariant()}.required_prefix = I
dotnet_naming_style.{CompilerExtensionsResources.Begins_with_I.Replace(' ', '_').ToLowerInvariant()}.required_suffix = 
dotnet_naming_style.{CompilerExtensionsResources.Begins_with_I.Replace(' ', '_').ToLowerInvariant()}.word_separator = 
dotnet_naming_style.{CompilerExtensionsResources.Begins_with_I.Replace(' ', '_').ToLowerInvariant()}.capitalization = pascal_case

dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.required_prefix = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.required_suffix = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.word_separator = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.capitalization = pascal_case

dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.required_prefix = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.required_suffix = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.word_separator = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.capitalization = pascal_case
";

    private readonly string ExpectedEditorConfigAfterAllSeveritiesChanged =
@$"
[*.{{cs,vb}}]
#### {CompilerExtensionsResources.Naming_styles} ####

# {CompilerExtensionsResources.Naming_rules}

dotnet_naming_rule.{(CompilerExtensionsResources.Interface + "_should_be_" + CompilerExtensionsResources.Begins_with_I.Replace(' ', '_')).ToLowerInvariant()}.severity = error
dotnet_naming_rule.{(CompilerExtensionsResources.Interface + "_should_be_" + CompilerExtensionsResources.Begins_with_I.Replace(' ', '_')).ToLowerInvariant()}.symbols = {CompilerExtensionsResources.Interface.ToLowerInvariant()}
dotnet_naming_rule.{(CompilerExtensionsResources.Interface + "_should_be_" + CompilerExtensionsResources.Begins_with_I.Replace(' ', '_')).ToLowerInvariant()}.style = {CompilerExtensionsResources.Begins_with_I.Replace(' ', '_').ToLowerInvariant()}

dotnet_naming_rule.{(CompilerExtensionsResources.Types + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.severity = error
dotnet_naming_rule.{(CompilerExtensionsResources.Types + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.symbols = {CompilerExtensionsResources.Types.ToLowerInvariant()}
dotnet_naming_rule.{(CompilerExtensionsResources.Types + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.style = {CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}

dotnet_naming_rule.{(CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_') + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.severity = error
dotnet_naming_rule.{(CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_') + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.symbols = {CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_').ToLowerInvariant()}
dotnet_naming_rule.{(CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_') + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.style = {CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}

# {CompilerExtensionsResources.Symbol_specifications}

dotnet_naming_symbols.{CompilerExtensionsResources.Interface.ToLowerInvariant()}.applicable_kinds = interface
dotnet_naming_symbols.{CompilerExtensionsResources.Interface.ToLowerInvariant()}.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
dotnet_naming_symbols.{CompilerExtensionsResources.Interface.ToLowerInvariant()}.required_modifiers = 

dotnet_naming_symbols.{CompilerExtensionsResources.Types.ToLowerInvariant()}.applicable_kinds = class, struct, interface, enum
dotnet_naming_symbols.{CompilerExtensionsResources.Types.ToLowerInvariant()}.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
dotnet_naming_symbols.{CompilerExtensionsResources.Types.ToLowerInvariant()}.required_modifiers = 

dotnet_naming_symbols.{CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_').ToLowerInvariant()}.applicable_kinds = property, event, method
dotnet_naming_symbols.{CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_').ToLowerInvariant()}.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
dotnet_naming_symbols.{CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_').ToLowerInvariant()}.required_modifiers = 

# {CompilerExtensionsResources.Naming_styles}

dotnet_naming_style.{CompilerExtensionsResources.Begins_with_I.Replace(' ', '_').ToLowerInvariant()}.required_prefix = I
dotnet_naming_style.{CompilerExtensionsResources.Begins_with_I.Replace(' ', '_').ToLowerInvariant()}.required_suffix = 
dotnet_naming_style.{CompilerExtensionsResources.Begins_with_I.Replace(' ', '_').ToLowerInvariant()}.word_separator = 
dotnet_naming_style.{CompilerExtensionsResources.Begins_with_I.Replace(' ', '_').ToLowerInvariant()}.capitalization = pascal_case

dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.required_prefix = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.required_suffix = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.word_separator = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.capitalization = pascal_case

dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.required_prefix = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.required_suffix = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.word_separator = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.capitalization = pascal_case
";

    private readonly string ExpectedEditorConfigAfterSymbolSpecChange =
@$"
[*.{{cs,vb}}]
#### {CompilerExtensionsResources.Naming_styles} ####

# {CompilerExtensionsResources.Naming_rules}

dotnet_naming_rule.{(CompilerExtensionsResources.Interface + "_should_be_" + CompilerExtensionsResources.Begins_with_I.Replace(' ', '_')).ToLowerInvariant()}.severity = error
dotnet_naming_rule.{(CompilerExtensionsResources.Interface + "_should_be_" + CompilerExtensionsResources.Begins_with_I.Replace(' ', '_')).ToLowerInvariant()}.symbols = {CompilerExtensionsResources.Interface.ToLowerInvariant()}
dotnet_naming_rule.{(CompilerExtensionsResources.Interface + "_should_be_" + CompilerExtensionsResources.Begins_with_I.Replace(' ', '_')).ToLowerInvariant()}.style = {CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}

dotnet_naming_rule.{(CompilerExtensionsResources.Types + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.severity = error
dotnet_naming_rule.{(CompilerExtensionsResources.Types + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.symbols = {CompilerExtensionsResources.Types.ToLowerInvariant()}
dotnet_naming_rule.{(CompilerExtensionsResources.Types + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.style = {CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}

dotnet_naming_rule.{(CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_') + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.severity = error
dotnet_naming_rule.{(CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_') + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.symbols = {CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_').ToLowerInvariant()}
dotnet_naming_rule.{(CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_') + "_should_be_" + CompilerExtensionsResources.Pascal_Case.Replace(' ', '_')).ToLowerInvariant()}.style = {CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}

# {CompilerExtensionsResources.Symbol_specifications}

dotnet_naming_symbols.{CompilerExtensionsResources.Interface.ToLowerInvariant()}.applicable_kinds = interface
dotnet_naming_symbols.{CompilerExtensionsResources.Interface.ToLowerInvariant()}.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
dotnet_naming_symbols.{CompilerExtensionsResources.Interface.ToLowerInvariant()}.required_modifiers = 

dotnet_naming_symbols.{CompilerExtensionsResources.Types.ToLowerInvariant()}.applicable_kinds = class, struct, interface, enum
dotnet_naming_symbols.{CompilerExtensionsResources.Types.ToLowerInvariant()}.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
dotnet_naming_symbols.{CompilerExtensionsResources.Types.ToLowerInvariant()}.required_modifiers = 

dotnet_naming_symbols.{CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_').ToLowerInvariant()}.applicable_kinds = property, event, method
dotnet_naming_symbols.{CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_').ToLowerInvariant()}.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
dotnet_naming_symbols.{CompilerExtensionsResources.Non_Field_Members.Replace(' ', '_').Replace('-', '_').ToLowerInvariant()}.required_modifiers = 

# {CompilerExtensionsResources.Naming_styles}

dotnet_naming_style.{CompilerExtensionsResources.Begins_with_I.Replace(' ', '_').ToLowerInvariant()}.required_prefix = I
dotnet_naming_style.{CompilerExtensionsResources.Begins_with_I.Replace(' ', '_').ToLowerInvariant()}.required_suffix = 
dotnet_naming_style.{CompilerExtensionsResources.Begins_with_I.Replace(' ', '_').ToLowerInvariant()}.word_separator = 
dotnet_naming_style.{CompilerExtensionsResources.Begins_with_I.Replace(' ', '_').ToLowerInvariant()}.capitalization = pascal_case

dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.required_prefix = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.required_suffix = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.word_separator = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.capitalization = pascal_case

dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.required_prefix = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.required_suffix = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.word_separator = 
dotnet_naming_style.{CompilerExtensionsResources.Pascal_Case.Replace(' ', '_').ToLowerInvariant()}.capitalization = pascal_case
";

    private class TestViewModel : ISettingsEditorViewModel
    {
        public void NotifyOfUpdate() { }

        Task<SourceText> ISettingsEditorViewModel.UpdateEditorConfigAsync(SourceText sourceText)
        {
            throw new NotImplementedException();
        }
    }
}
