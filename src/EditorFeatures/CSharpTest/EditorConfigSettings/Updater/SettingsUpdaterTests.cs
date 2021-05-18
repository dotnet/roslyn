// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    [UseExportProvider]
    public partial class SettingsUpdaterTests : TestBase
    {
        private static async Task<Workspace> CreateWorkspaceWithProjectAndDocumentsAsync()
        {
            var projectId = ProjectId.CreateNewId();
            var workspace = new AdhocWorkspace(EditorTestCompositions.EditorFeatures.GetHostServices(), WorkspaceKind.Host);

            var analyzerType = typeof(CSharpUseExplicitTypeDiagnosticAnalyzer);
            var analyzerReference = new AnalyzerFileReference(analyzerType.Assembly.Location, new TestAnalyzerAssemblyLoader());

            Assert.True(workspace.TryApplyChanges(workspace.CurrentSolution
                .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp)
                .AddDocument(DocumentId.CreateNewId(projectId), "goo.cs", "public class Goo { }")
                .AddAdditionalDocument(DocumentId.CreateNewId(projectId), "add.txt", "text")
                .AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId), "editorcfg", SourceText.From(""), filePath: "/a/b/config")));

            var project = workspace.CurrentSolution.Projects.First();
            var runner = new InProcOrRemoteHostAnalyzerRunner(new DiagnosticAnalyzerInfoCache());
            var compilationWithAnalyzers = (await project.GetCompilationAsync())?.WithAnalyzers(
                    analyzerReference.GetAnalyzers(project.Language).ToImmutableArray(),
                    new WorkspaceAnalyzerOptions(project.AnalyzerOptions, project.Solution));

            // no result for open file only analyzer unless forced
            _ = await runner.AnalyzeProjectAsync(project, compilationWithAnalyzers!, forceExecuteAllAnalyzers: true, logPerformanceInfo: false, getTelemetryInfo: false, cancellationToken: CancellationToken.None);

            return workspace;
        }

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
            using var workspace = await CreateWorkspaceWithProjectAndDocumentsAsync();
            var analyzerConfigDocument = CreateAnalyzerConfigDocument(workspace, initialEditorConfig);
            var sourcetext = await analyzerConfigDocument.GetTextAsync(default);
            var result = SettingsUpdateHelper.TryUpdateAnalyzerConfigDocument(sourcetext, analyzerConfigDocument.FilePath!, workspace.Options, options);
            Assert.Equal(updatedEditorConfig, result?.ToString());
        }

        private static async Task TestAsync(string initialEditorConfig, string updatedEditorConfig, params (AnalyzerSetting, DiagnosticSeverity)[] options)
        {
            using var workspace = await CreateWorkspaceWithProjectAndDocumentsAsync();
            var analyzerConfigDocument = CreateAnalyzerConfigDocument(workspace, initialEditorConfig);
            var sourcetext = await analyzerConfigDocument.GetTextAsync(default);
            var result = SettingsUpdateHelper.TryUpdateAnalyzerConfigDocument(sourcetext, analyzerConfigDocument.FilePath!, options);
            Assert.Equal(updatedEditorConfig, result?.ToString());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public async Task TestAddNewWhitespaceOptionAsync()
        {
            await TestAsync(
                string.Empty,
                "[*.cs]\r\ncsharp_new_line_before_else=true",
                (CSharpFormattingOptions2.NewLineForElse, true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public async Task TestAddNewBoolCodeStyleOptionWithSeverityAsync()
        {
            var option = CSharpCodeStyleOptions.PreferThrowExpression.DefaultValue;
            option.Value = true;
            option.Notification = CodeStyle.NotificationOption2.Suggestion;
            await TestAsync(
                string.Empty,
                @"[*.cs]
csharp_style_throw_expression=true
dotnet_diagnostic.IDE0016.severity=suggestion",
                (CSharpCodeStyleOptions.PreferThrowExpression, option));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public async Task TestAddNewEnumCodeStyleOptionWithSeverityOldOptionFormatExistAsync()
        {
            var option = CSharpCodeStyleOptions.PreferredUsingDirectivePlacement.DefaultValue;
            option.Value = AddImportPlacement.InsideNamespace;
            option.Notification = CodeStyle.NotificationOption2.Warning;
            await TestAsync(
                @"[*.cs]
csharp_using_directive_placement=inside_namespace:silent",
                @"[*.cs]
csharp_using_directive_placement=inside_namespace
dotnet_diagnostic.IDE0065.severity=warning",
                (CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, option));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public async Task TestAddNewEnumCodeStyleOptionWithSeverityOldOptionFormatExistHeadingMatchesAllFileTypesAsync()
        {
            var option = CSharpCodeStyleOptions.PreferredUsingDirectivePlacement.DefaultValue;
            option.Value = AddImportPlacement.InsideNamespace;
            option.Notification = CodeStyle.NotificationOption2.Warning;
            await TestAsync(
                @"[*]
csharp_using_directive_placement=inside_namespace:silent",
                @"[*]
csharp_using_directive_placement=inside_namespace
dotnet_diagnostic.IDE0065.severity=warning",
                (CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, option));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public async Task TestAddNewEnumCodeStyleOptionWithSeverityAsync()
        {
            var option = CSharpCodeStyleOptions.PreferredUsingDirectivePlacement.DefaultValue;
            option.Value = AddImportPlacement.InsideNamespace;
            option.Notification = CodeStyle.NotificationOption2.Warning;
            await TestAsync(
                string.Empty,
                "[*.cs]\r\ncsharp_using_directive_placement=inside_namespace\r\ndotnet_diagnostic.IDE0065.severity=warning",
                (CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, option));
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        internal async Task TestAddNewAnalyzerOptionOptionAsync(
            [CombinatorialValues(Language.CSharp, Language.VisualBasic, (Language.CSharp | Language.VisualBasic))]
            Language language,
            [CombinatorialValues(DiagnosticSeverity.Warning, DiagnosticSeverity.Error, DiagnosticSeverity.Info, DiagnosticSeverity.Hidden)]
            DiagnosticSeverity severity)
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
            var analyzerSetting = new AnalyzerSetting(descriptor, ReportDiagnostic.Suppress, null!, language);

            await TestAsync(
                string.Empty,
                $"{expectedHeader}\r\ndotnet_diagnostic.{id}.severity={expectedSeverity}",
                (analyzerSetting, severity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public async Task TestUpdateExistingWhitespaceOptionAsync()
        {
            await TestAsync(
                "[*.cs]\r\ncsharp_new_line_before_else=true",
                "[*.cs]\r\ncsharp_new_line_before_else=false",
                (CSharpFormattingOptions2.NewLineForElse, false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public async Task TestAddNewWhitespaceOptionToExistingFileAsync()
        {
            var initialEditorConfig = @"
[*.{cs,vb}]

# CA1000: Do not declare static members on generic types
dotnet_diagnostic.CA1000.severity=false

";

            var updatedEditorConfig = @"
[*.{cs,vb}]

# CA1000: Do not declare static members on generic types
dotnet_diagnostic.CA1000.severity=false


[*.cs]
csharp_new_line_before_else=true";
            await TestAsync(
                initialEditorConfig,
                updatedEditorConfig,
                (CSharpFormattingOptions2.NewLineForElse, true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
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
csharp_new_line_before_else=true";
            await TestAsync(
                initialEditorConfig,
                updatedEditorConfig,
                (CSharpFormattingOptions2.NewLineForElse, true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
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
csharp_new_line_before_else=true";

            await TestAsync(
                initialEditorConfig,
                updatedEditorConfig,
                (CSharpFormattingOptions2.NewLineForElse, true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public async Task TestAddMultimpleNewWhitespaceOptions()
        {
            await TestAsync(
                string.Empty,
                @"[*.cs]
csharp_new_line_before_else=true
csharp_new_line_before_catch=true
csharp_new_line_before_finally=true",
                (CSharpFormattingOptions2.NewLineForElse, true),
                (CSharpFormattingOptions2.NewLineForCatch, true),
                (CSharpFormattingOptions2.NewLineForFinally, true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
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
dotnet_sort_system_directives_first=true

# CSharp code style settings:
[*.cs]";

            await TestAsync(
                initialEditorConfig,
                updatedEditorConfig,
                (GenerationOptions.PlaceSystemNamespaceFirst, true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
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
csharp_new_line_before_else=true";

            await TestAsync(
                initialEditorConfig,
                updatedEditorConfig,
                (CSharpFormattingOptions2.NewLineForElse, true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public async Task TestAnalyzerSettingsUpdaterService()
        {
            var workspace = await CreateWorkspaceWithProjectAndDocumentsAsync();
            var updater = new AnalyzerSettingsUpdater(workspace, "/a/b/config");
            var id = "Test001";
            var descriptor = new DiagnosticDescriptor(id: id, title: "", messageFormat: "", category: "Naming", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: false);
            var analyzerSetting = new AnalyzerSetting(descriptor, ReportDiagnostic.Suppress, updater, Language.CSharp);
            analyzerSetting.ChangeSeverity(DiagnosticSeverity.Error);
            var updates = await updater.GetChangedEditorConfigAsync(default);
            var update = Assert.Single(updates);
            Assert.Equal($@"[*.cs]
dotnet_diagnostic.{id}.severity=error", update.NewText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public async Task TestCodeStyleSettingUpdaterService()
        {
            var workspace = await CreateWorkspaceWithProjectAndDocumentsAsync();
            var updater = new OptionUpdater(workspace, "/a/b/config");
            var setting = CodeStyleSetting.Create(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer,
                                                  "",
                                                  TestAnalyzerConfigOptions.Instance,
                                                  workspace.Options,
                                                  updater);
            setting.ChangeSeverity(DiagnosticSeverity.Error);
            var updates = await updater.GetChangedEditorConfigAsync(default);
            var update = Assert.Single(updates);
            var actual = update.NewText;
            var expected = @"[*.cs]
csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental=true
dotnet_diagnostic.IDE2004.severity=error";
            Assert.Equal(expected, actual);
            setting.ChangeValue(1);
            updates = await updater.GetChangedEditorConfigAsync(default);
            update = Assert.Single(updates);
            actual = update.NewText;
            expected = @"[*.cs]
csharp_style_allow_blank_line_after_colon_in_constructor_initializer_experimental=false
dotnet_diagnostic.IDE2004.severity=error";
            Assert.Equal(expected, actual);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public async Task TestFormattingSettingUpdaterService()
        {
            var workspace = await CreateWorkspaceWithProjectAndDocumentsAsync();
            var updater = new OptionUpdater(workspace, "/a/b/config");
            var setting = FormattingSetting.Create(CSharpFormattingOptions2.NewLineForElse, "", TestAnalyzerConfigOptions.Instance, workspace.Options, updater);
            setting.SetValue(false);
            var updates = await updater.GetChangedEditorConfigAsync(default);
            var update = Assert.Single(updates);
            Assert.Equal(@"[*.cs]
csharp_new_line_before_else=false", update.NewText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public async Task TestCodeStyleOption_BlankFile_Severity()
        {
            var workspace = await CreateWorkspaceWithProjectAndDocumentsAsync();
            var updater = new OptionUpdater(workspace, "/a/b/config");
            var ids = IDEDiagnosticIdToOptionMappingHelper.GetDiagnosticIdsForOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, LanguageNames.CSharp);
            Assert.Equal(2, ids.Length);
            var setting = CodeStyleSetting.Create(CSharpCodeStyleOptions.VarWhenTypeIsApparent,
                                                  "",
                                                  TestAnalyzerConfigOptions.Instance,
                                                  workspace.Options,
                                                  updater);
            setting.ChangeSeverity(DiagnosticSeverity.Error);
            var updates = await updater.GetChangedEditorConfigAsync(default);
            var update = Assert.Single(updates);
            var value = setting.Value?.ToString().ToLower();
            var expected = $@"[*.cs]
csharp_style_var_when_type_is_apparent={value}
dotnet_diagnostic.{ids[0]}.severity=error
dotnet_diagnostic.{ids[1]}.severity=error";
            var actual = update.NewText;
            Assert.Equal(expected, actual);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public async Task TestCodeStyleOption_BlankFile_Value()
        {
            var workspace = await CreateWorkspaceWithProjectAndDocumentsAsync();
            var updater = new OptionUpdater(workspace, "/a/b/config");
            var ids = IDEDiagnosticIdToOptionMappingHelper.GetDiagnosticIdsForOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, LanguageNames.CSharp);
            Assert.Equal(2, ids.Length);
            var setting = CodeStyleSetting.Create(CSharpCodeStyleOptions.VarWhenTypeIsApparent,
                                                  "",
                                                  TestAnalyzerConfigOptions.Instance,
                                                  workspace.Options,
                                                  updater);
            setting.ChangeValue(0);
            var updates = await updater.GetChangedEditorConfigAsync(default);
            var update = Assert.Single(updates);
            var expected = $@"[*.cs]
csharp_style_var_when_type_is_apparent=true
dotnet_diagnostic.{ids[0]}.severity=silent
dotnet_diagnostic.{ids[1]}.severity=silent";
            var actual = update.NewText;
            Assert.Equal(expected, actual);
        }
    }
}
