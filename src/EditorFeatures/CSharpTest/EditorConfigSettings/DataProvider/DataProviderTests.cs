// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
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
            var workspace = new AdhocWorkspace(EditorTestCompositions.EditorFeatures.GetHostServices(), WorkspaceKind.Host);
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
        public void TestGettingFormattingSettingsProvider()
        {
            TestGettingSettingsProviderFromWorkspace<FormattingSetting>();
            TestGettingSettingsProviderFromLanguageService<FormattingSetting>();
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
            Assert.Equal(26, dataSnapShot.Length);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingCodeStyleSettingsProviderLanguageServiceAsync()
        {
            var settingsProviderFactory = GettingSettingsProviderFactoryFromLanguageService<CodeStyleSetting>(LanguageNames.CSharp);
            var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
            var model = new TestViewModel();
            settingsProvider.RegisterViewModel(model);
            var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
            Assert.Equal(29, dataSnapShot.Length);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingFormattingSettingProviderWorkspaceServiceAsync()
        {
            var settingsProviderFactory = GettingSettingsProviderFactoryFromWorkspace<FormattingSetting>();
            var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
            var model = new TestViewModel();
            settingsProvider.RegisterViewModel(model);
            var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
            Assert.Equal(5, dataSnapShot.Length);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingFormattingSettingProviderLanguageServiceAsync()
        {
            var settingsProviderFactory = GettingSettingsProviderFactoryFromLanguageService<FormattingSetting>(LanguageNames.CSharp);
            var settingsProvider = settingsProviderFactory.GetForFile("/a/b/config");
            var model = new TestViewModel();
            settingsProvider.RegisterViewModel(model);
            var dataSnapShot = settingsProvider.GetCurrentDataSnapshot();
            Assert.Equal(47, dataSnapShot.Length);
        }
    }
}
