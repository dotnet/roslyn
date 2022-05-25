// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditorConfigSettings.DataProvider
{
    [UseExportProvider]
    public partial class DataProviderTests
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

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingAnalyzerSettingsProvider()
        {
            TestGettingSettingsProviderFromWorkspace<AnalyzerSetting>();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingCodeStyleSettingsProvider()
        {
            TestGettingSettingsProviderFromWorkspace<CodeStyleSetting>();
            TestGettingSettingsProviderFromLanguageService<CodeStyleSetting>();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingWhitespaceSettingsProvider()
        {
            TestGettingSettingsProviderFromWorkspace<WhitespaceSetting>();
            TestGettingSettingsProviderFromLanguageService<WhitespaceSetting>();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingNamingStyleSettingsProvider()
        {
            TestGettingSettingsProviderFromWorkspace<NamingStyleSetting>();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingCodeStyleSettingProviderWorkspaceServiceAsync()
        {
            var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspace<CodeStyleSetting>();
            var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
            var model = new TestViewModel();
            settingsProvider.RegisterViewModel(model);
            var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
            // We need to substract as a UI for arbitrary strings for:
            //
            // CodeStyleOptions2.OperatorPlacementWhenWrapping
            // CodeStyleOptions2.FileHeaderTemplate
            // CodeStyleOptions2.ForEachExplicitCastInSource
            var optionsCount = CodeStyleOptions2.AllOptions.Where(x => x.StorageLocations.Any(y => y is IEditorConfigStorageLocation2)).Count() - 3;
            Assert.Equal(optionsCount, dataSnapShot.Length);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingCodeStyleSettingsProviderLanguageServiceAsync()
        {
            var settingsProviderFactory = GettingSettingsProviderFactoryFromLanguageService<CodeStyleSetting>(LanguageNames.CSharp);
            var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
            var model = new TestViewModel();
            settingsProvider.RegisterViewModel(model);
            var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
            // We don't support PreferredModifierOrder yet so we subtract by one
            var optionsCount = CSharpCodeStyleOptions.AllOptions.Where(x => x.StorageLocations.Any(y => y is IEditorConfigStorageLocation2)).Count() - 1;
            Assert.Equal(optionsCount, dataSnapShot.Length);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingWhitespaceSettingProviderWorkspaceServiceAsync()
        {
            var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspace<WhitespaceSetting>();
            var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
            var model = new TestViewModel();
            settingsProvider.RegisterViewModel(model);
            var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();

            var expectedOptions = new IOption[]
            {
                FormattingOptions2.IndentationSize,
                FormattingOptions2.InsertFinalNewLine,
                FormattingOptions2.NewLine,
                FormattingOptions2.TabSize,
                FormattingOptions2.UseTabs,
                CodeStyleOptions2.OperatorPlacementWhenWrapping
            };

            AssertEx.SetEqual(
                expectedOptions.Select(option => option.Name),
                dataSnapShot.Select(item => item.Key.Option.Name));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingWhitespaceSettingProviderLanguageServiceAsync()
        {
            var settingsProviderFactory = GettingSettingsProviderFactoryFromLanguageService<WhitespaceSetting>(LanguageNames.CSharp);
            var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
            var model = new TestViewModel();
            settingsProvider.RegisterViewModel(model);
            var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
            var optionsCount = CSharpFormattingOptions2.AllOptions.Where(x => x.StorageLocations.Any(y => y is IEditorConfigStorageLocation2)).Count();
            Assert.Equal(optionsCount, dataSnapShot.Length);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingSettingProviderWithNullProjectPath1()
        {
            var settingsProviderFactory = GettingSettingsProviderFactoryFromLanguageServiceWithNullProjectPath<WhitespaceSetting>(LanguageNames.CSharp);
            var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
            var model = new TestViewModel();
            settingsProvider.RegisterViewModel(model);
            var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
            Assert.Empty(dataSnapShot);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingSettingProviderWithNullProjectPath2()
        {
            var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspaceWithNullProjectPath<WhitespaceSetting>();
            var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
            var model = new TestViewModel();
            settingsProvider.RegisterViewModel(model);
            var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
            Assert.Empty(dataSnapShot);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingSettingProviderWithNullProjectPath3()
        {
            var settingsProviderFactory = GettingSettingsProviderFactoryFromLanguageServiceWithNullProjectPath<CodeStyleSetting>(LanguageNames.CSharp);
            var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
            var model = new TestViewModel();
            settingsProvider.RegisterViewModel(model);
            var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
            Assert.Empty(dataSnapShot);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingSettingProviderWithNullProjectPath4()
        {
            var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspaceWithNullProjectPath<CodeStyleSetting>();
            var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
            var model = new TestViewModel();
            settingsProvider.RegisterViewModel(model);
            var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
            Assert.Empty(dataSnapShot);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingSettingProviderWithNullProjectPath5()
        {
            var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspaceWithNullProjectPath<AnalyzerSetting>();
            var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
            var model = new TestViewModel();
            settingsProvider.RegisterViewModel(model);
            var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
            Assert.Empty(dataSnapShot);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
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
}
