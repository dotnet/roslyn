// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditorConfigSettings.Aggregator
{
    [UseExportProvider]
    public class SettingsAggregatorTests
    {
        public static Workspace CreateWorkspace(params Type[]? additionalParts)
            => new AdhocWorkspace(EditorTestCompositions.EditorFeatures.AddParts(additionalParts).GetHostServices(), WorkspaceKind.Host);

        private static Workspace CreateWorkspaceWithProjectAndDocuments()
        {
            var projectId = ProjectId.CreateNewId();

            var workspace = CreateWorkspace();

            Assert.True(workspace.TryApplyChanges(workspace.CurrentSolution
                .AddProject(projectId, "proj1", "proj1.dll", LanguageNames.CSharp)
                .AddDocument(DocumentId.CreateNewId(projectId), "goo.cs", "public class Goo { }")
                .AddAdditionalDocument(DocumentId.CreateNewId(projectId), "add.txt", "text")
                .AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId), "editorcfg", SourceText.From("config"), filePath: "/a/b")));

            return workspace;
        }

        private static void TestGettingProvider<T>()
        {
            var workspace = CreateWorkspaceWithProjectAndDocuments();
            var settingsAggregator = workspace.Services.GetRequiredService<ISettingsAggregator>();
            var settingsProvider = settingsAggregator.GetSettingsProvider<T>("/a/b/config");
            Assert.NotNull(settingsProvider);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingCodeStyleProvider() => TestGettingProvider<CodeStyleSetting>();

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingAnalyzerProvider() => TestGettingProvider<AnalyzerSetting>();

        [Fact, Trait(Traits.Feature, Traits.Features.EditorConfigUI)]
        public void TestGettingFormattingProvider() => TestGettingProvider<FormattingSetting>();
    }
}
