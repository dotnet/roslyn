// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditorConfigSettings.DataProvider;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
public sealed partial class DataProviderTests
{
    private static Workspace GetWorkspace(string? projectFilePath = null)
    {
        var projectId = ProjectId.CreateNewId();
        var workspace = new AdhocWorkspace(VisualStudioTestCompositions.LanguageServices.GetHostServices(), WorkspaceKind.Host);
        Assert.True(workspace.TryApplyChanges(workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Create(), "proj1", "proj1.dll", LanguageNames.CSharp, filePath: projectFilePath))
            .AddDocument(DocumentId.CreateNewId(projectId), "goo.cs", "public class Goo { }")
            .AddAdditionalDocument(DocumentId.CreateNewId(projectId), "add.txt", "text")
            .AddAnalyzerReference(projectId, new MockAnalyzerReference())
            .AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId), "editorcfg", SourceText.From("config"), filePath: "/a/b")));
        return workspace;
    }

    private static IWorkspaceSettingsProviderFactory<T> GettingSettingsProviderFactoryFromWorkspace<T>()
        => GetWorkspace("/a/b/proj1.csproj").Services.GetRequiredService<IWorkspaceSettingsProviderFactory<T>>();

    private static ILanguageSettingsProviderFactory<T> GettingSettingsProviderFactoryFromLanguageService<T>(string languageName)
        => GetWorkspace("/a/b/proj1.csproj").Services.GetLanguageServices(languageName).GetRequiredService<ILanguageSettingsProviderFactory<T>>();

    private static IWorkspaceSettingsProviderFactory<T> GettingSettingsProviderFactoryFromWorkspaceWithNullProjectPath<T>()
        => GetWorkspace().Services.GetRequiredService<IWorkspaceSettingsProviderFactory<T>>();

    private static ILanguageSettingsProviderFactory<T> GettingSettingsProviderFactoryFromLanguageServiceWithNullProjectPath<T>(string languageName)
        => GetWorkspace().Services.GetLanguageServices(languageName).GetRequiredService<ILanguageSettingsProviderFactory<T>>();

    private static ISettingsProvider<T> TestGettingSettingsProviderFromWorkspace<T>()
    {
        var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspace<T>();
        var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
        Assert.NotNull(settingsProvider);
        return settingsProvider;
    }

    private static ISettingsProvider<T> TestGettingSettingsProviderFromLanguageService<T>()
    {
        var settingsProviderFactory = GettingSettingsProviderFactoryFromLanguageService<T>(LanguageNames.CSharp);
        var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
        Assert.NotNull(settingsProvider);
        return settingsProvider;
    }

    [Fact]
    public void TestGettingAnalyzerSettingsProvider()
        => TestGettingSettingsProviderFromWorkspace<AnalyzerSetting>();

    [Fact]
    public void TestGettingCodeStyleSettingsProvider()
    {
        TestGettingSettingsProviderFromWorkspace<CodeStyleSetting>();
        TestGettingSettingsProviderFromLanguageService<CodeStyleSetting>();
    }

    [Fact]
    public void TestGettingWhitespaceSettingsProvider()
    {
        TestGettingSettingsProviderFromWorkspace<Setting>();
        TestGettingSettingsProviderFromLanguageService<Setting>();
    }

    [Fact]
    public void TestGettingNamingStyleSettingsProvider()
        => TestGettingSettingsProviderFromWorkspace<NamingStyleSetting>();

    [Fact]
    public void TestGettingAnalyzerSettingsProviderWorkspaceServiceAsync()
    {
        var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspace<AnalyzerSetting>();
        var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
        var model = new TestViewModel();
        settingsProvider.RegisterViewModel(model);
        var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
        var result = Assert.Single(dataSnapShot);
        Assert.Equal("MyDiagnostic", result.Id);
        Assert.Equal("MockDiagnostic", result.Title);
        Assert.Equal(string.Empty, result.Description);
        Assert.Equal("InternalCategory", result.Category);
        Assert.True(result.IsEnabled);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/62937")]
    public void TestGettingCodeStyleSettingProviderWorkspaceServiceAsync()
    {
        var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspace<CodeStyleSetting>();
        var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
        var model = new TestViewModel();
        settingsProvider.RegisterViewModel(model);
        var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();

        // We need to substract for string options that are not yet supported.
        // https://github.com/dotnet/roslyn/issues/62937
        var optionsWithUI = CodeStyleOptions2.EditorConfigOptions
            .Remove(CodeStyleOptions2.PreferSystemHashCode)
            .Remove(CodeStyleOptions2.OperatorPlacementWhenWrapping)
            .Remove(CodeStyleOptions2.FileHeaderTemplate)
            .Remove(CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions)
            .Remove(CodeStyleOptions2.ForEachExplicitCastInSource);

        AssertEx.Equal(
            optionsWithUI.OrderBy(o => o.Definition.ConfigName),
            dataSnapShot.Select(setting => setting.Key.Option).OrderBy(o => o.Definition.ConfigName));
    }

    [Fact]
    public void TestGettingNamingStyleSettingProviderWorkspaceServiceAsync()
    {
        var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspace<NamingStyleSetting>();
        var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
        var model = new TestViewModel();
        settingsProvider.RegisterViewModel(model);
        var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
        Assert.Collection(dataSnapShot,
            namingStyle1 =>
            {
                Assert.Equal(CompilerExtensionsResources.Begins_with_I, namingStyle1.StyleName);
                Assert.Equal(CompilerExtensionsResources.Interface, namingStyle1.TypeName);
                Assert.Equal(ReportDiagnostic.Info, namingStyle1.Severity);
            },
            namingStyle2 =>
            {
                Assert.Equal(CompilerExtensionsResources.Pascal_Case, namingStyle2.StyleName);
                Assert.Equal(CompilerExtensionsResources.Types, namingStyle2.TypeName);
                Assert.Equal(ReportDiagnostic.Info, namingStyle2.Severity);
            },
            namingStyle3 =>
            {
                Assert.Equal(CompilerExtensionsResources.Pascal_Case, namingStyle3.StyleName);
                Assert.Equal(CompilerExtensionsResources.Non_Field_Members, namingStyle3.TypeName);
                Assert.Equal(ReportDiagnostic.Info, namingStyle3.Severity);
            });
    }

    [Fact]
    public void TestGettingCodeStyleSettingsProviderLanguageServiceAsync()
    {
        var settingsProviderFactory = GettingSettingsProviderFactoryFromLanguageService<CodeStyleSetting>(LanguageNames.CSharp);
        var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
        var model = new TestViewModel();
        settingsProvider.RegisterViewModel(model);
        var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();

        // We don't support PreferredModifierOrder yet:
        var optionsWithUI = CSharpCodeStyleOptions.EditorConfigOptions
            .Remove(CSharpCodeStyleOptions.PreferredModifierOrder);

        AssertEx.SetEqual(optionsWithUI.OrderBy(o => o.Name), dataSnapShot.Select(setting => setting.Key.Option).OrderBy(o => o.Name));
    }

    [Fact]
    public void TestGettingWhitespaceSettingProviderWorkspaceServiceAsync()
    {
        var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspace<Setting>();
        var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
        var model = new TestViewModel();
        settingsProvider.RegisterViewModel(model);
        var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();

        var expectedOptions = new IOption2[]
        {
            FormattingOptions2.IndentationSize,
            FormattingOptions2.InsertFinalNewLine,
            FormattingOptions2.NewLine,
            FormattingOptions2.TabSize,
            FormattingOptions2.UseTabs,
            CodeStyleOptions2.OperatorPlacementWhenWrapping
        };

        AssertEx.Equal(
            expectedOptions.Select(option => option.Definition.ConfigName).OrderBy(n => n),
            dataSnapShot.Select(item => item.Key.Option.Definition.ConfigName).OrderBy(n => n));
    }

    [Fact]
    public void TestGettingWhitespaceSettingProviderLanguageServiceAsync()
    {
        var settingsProviderFactory = GettingSettingsProviderFactoryFromLanguageService<Setting>(LanguageNames.CSharp);
        var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
        var model = new TestViewModel();
        settingsProvider.RegisterViewModel(model);
        var dataSnapshot = settingsProvider.GetCurrentDataSnapshot();

        // multiple settings may share the same option (e.g. settings representing flags of an enum):
        var optionsForSettings = dataSnapshot.GroupBy(s => s.Key.Option).Select(g => g.Key).ToArray();
        AssertEx.SetEqual(CSharpFormattingOptions2.EditorConfigOptions, optionsForSettings);
    }

    [Fact]
    public void TestGettingSettingProviderWithNullProjectPath1()
    {
        var settingsProviderFactory = GettingSettingsProviderFactoryFromLanguageServiceWithNullProjectPath<Setting>(LanguageNames.CSharp);
        var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
        var model = new TestViewModel();
        settingsProvider.RegisterViewModel(model);
        var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
        Assert.Empty(dataSnapShot);
    }

    [Fact]
    public void TestGettingSettingProviderWithNullProjectPath2()
    {
        var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspaceWithNullProjectPath<Setting>();
        var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
        var model = new TestViewModel();
        settingsProvider.RegisterViewModel(model);
        var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
        Assert.Empty(dataSnapShot);
    }

    [Fact]
    public void TestGettingSettingProviderWithNullProjectPath3()
    {
        var settingsProviderFactory = GettingSettingsProviderFactoryFromLanguageServiceWithNullProjectPath<CodeStyleSetting>(LanguageNames.CSharp);
        var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
        var model = new TestViewModel();
        settingsProvider.RegisterViewModel(model);
        var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
        Assert.Empty(dataSnapShot);
    }

    [Fact]
    public void TestGettingSettingProviderWithNullProjectPath4()
    {
        var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspaceWithNullProjectPath<CodeStyleSetting>();
        var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
        var model = new TestViewModel();
        settingsProvider.RegisterViewModel(model);
        var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
        Assert.Empty(dataSnapShot);
    }

    [Fact]
    public void TestGettingSettingProviderWithNullProjectPath5()
    {
        var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspaceWithNullProjectPath<AnalyzerSetting>();
        var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
        var model = new TestViewModel();
        settingsProvider.RegisterViewModel(model);
        var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
        Assert.Empty(dataSnapShot);
    }

    [Fact]
    public void TestGettingSettingProviderWithNullProjectPath6()
    {
        var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspaceWithNullProjectPath<NamingStyleSetting>();
        var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
        var model = new TestViewModel();
        settingsProvider.RegisterViewModel(model);
        var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
        Assert.Empty(dataSnapShot);
    }
}
