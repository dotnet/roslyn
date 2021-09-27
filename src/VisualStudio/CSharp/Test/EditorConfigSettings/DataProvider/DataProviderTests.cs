// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Editor.UnitTests;
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
        private static Workspace GetWorkspace()
        {
            var projectId = ProjectId.CreateNewId();
            var workspace = new AdhocWorkspace(VisualStudioTestCompositions.LanguageServices.GetHostServices(), WorkspaceKind.Host);
            Assert.True(workspace.TryApplyChanges(workspace.CurrentSolution
                .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp)
                .AddDocument(DocumentId.CreateNewId(projectId), "goo.cs", "public class Goo { }")
                .AddAdditionalDocument(DocumentId.CreateNewId(projectId), "add.txt", "text")
                .AddAnalyzerReference(projectId, new MockAnalyzerReference())
                .AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId), "editorcfg", SourceText.From("config"), filePath: "/a/b")));
            return workspace;
        }

        private static IWorkspaceSettingsProviderFactory<T> GettingSettingsProviderFactoryFromWorkspace<T>()
            => GetWorkspace().Services.GetRequiredService<IWorkspaceSettingsProviderFactory<T>>();

        private static ILanguageSettingsProviderFactory<T> GettingSettingsProviderFactoryFromLanguageService<T>(string languageName)
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
            // CodeStyleOptions2.OperatorPlacementWhenWrapping is included in whitespace options so we need to substract one
            // We do not yet support the following options as they are strings and we need to build a UI to show arbitrary strings:
            // CodeStyleOptions2.FileHeaderTemplate
            var optionsCount = CodeStyleOptions2.AllOptions.Where(x => x.StorageLocations.Any(y => y is IEditorConfigStorageLocation2)).Count() - 2;
            Assert.Equal(optionsCount, dataSnapShot.Length);
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
            var optionsCount = FormattingOptions2.AllOptions.Where(x => x.StorageLocations.Any(y => y is IEditorConfigStorageLocation2)).Count();
            // we also include CodeStyleOptions2.OperatorPlacementWhenWrapping so we need to add one
            optionsCount += 1;
            Assert.Equal(optionsCount, dataSnapShot.Length);
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
    }
}
