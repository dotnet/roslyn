// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.Formatting)]
public class CSharpNewDocumentFormatting : AbstractIntegrationTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync().ConfigureAwait(true);
        await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(CSharpNewDocumentFormatting), HangMitigatingCancellationToken);
        await TestServices.Workspace.SetFullSolutionAnalysisAsync(false, HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/79302"), WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1411721")]
    public async Task CreateLegacyProjectWithFileScopedNamespaces()
    {
        await TestServices.Workspace.SetFileScopedNamespaceAsync(true, HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);

        await VerifyNoErrorsAsync(HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63620")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1411721")]
    public async Task CreateSDKProjectWithFileScopedNamespaces()
    {
        await TestServices.Workspace.SetFileScopedNamespaceAsync(true, HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.CSharpNetCoreConsoleApplication, LanguageNames.CSharp, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], HangMitigatingCancellationToken);

        await VerifyNoErrorsAsync(HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63620")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/60449")]
    public async Task CreateSDKProjectWithBlockScopedNamespacesFromEditorConfig()
    {
        await TestServices.Workspace.SetFileScopedNamespaceAsync(true, HangMitigatingCancellationToken);

        var (solutionDirectory, _, _) = await TestServices.SolutionExplorer.GetSolutionInfoAsync(HangMitigatingCancellationToken);
        var editorConfigFilePath = Path.Combine(solutionDirectory, ".editorconfig");
        File.WriteAllText(editorConfigFilePath,
            """

            root = true

            [*.cs]
            csharp_style_namespace_declarations = block_scoped

            """);

        await TestServices.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.CSharpNetCoreClassLibrary, LanguageNames.CSharp, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], HangMitigatingCancellationToken);

        await VerifyNoErrorsAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("namespace TestProj\r\n{");
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63620")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/60449")]
    public async Task CreateSDKProjectWithBlockScopedNamespacesFromIrrelevantEditorConfigH()
    {
        await TestServices.Workspace.SetFileScopedNamespaceAsync(true, HangMitigatingCancellationToken);

        var (solutionDirectory, _, _) = await TestServices.SolutionExplorer.GetSolutionInfoAsync(HangMitigatingCancellationToken);
        var editorConfigFilePath = Path.Combine(solutionDirectory, ".editorconfig");
        File.WriteAllText(editorConfigFilePath,
            """

            root = true

            """);

        // This editor config file should be ignored
        editorConfigFilePath = Path.Combine(solutionDirectory, "..", ".editorconfig");
        File.WriteAllText(editorConfigFilePath,
            """

            [*.cs]
            csharp_style_namespace_declarations = block_scoped

            """);

        await TestServices.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.CSharpNetCoreClassLibrary, LanguageNames.CSharp, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], HangMitigatingCancellationToken);

        await VerifyNoErrorsAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("namespace TestProj;");
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63620")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/60449")]
    public async Task CreateSDKProjectWithFileScopedNamespacesFromEditorConfig()
    {
        await TestServices.Workspace.SetFileScopedNamespaceAsync(false, HangMitigatingCancellationToken);

        var (solutionDirectory, _, _) = await TestServices.SolutionExplorer.GetSolutionInfoAsync(HangMitigatingCancellationToken);
        var editorConfigFilePath = Path.Combine(solutionDirectory, ".editorconfig");
        File.WriteAllText(editorConfigFilePath,
            """

            root = true

            [*.cs]
            csharp_style_namespace_declarations = file_scoped

            """);

        await TestServices.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.CSharpNetCoreClassLibrary, LanguageNames.CSharp, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace], HangMitigatingCancellationToken);

        await VerifyNoErrorsAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("namespace TestProj;");
    }

    private async Task VerifyNoErrorsAsync(CancellationToken cancellationToken)
    {
        await TestServices.ErrorList.ShowErrorListAsync(cancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles, FeatureAttribute.ErrorList], cancellationToken);
        var actualContents = await TestServices.ErrorList.GetErrorsAsync(cancellationToken);
        AssertEx.EqualOrDiff(
            string.Join<string>(Environment.NewLine, []),
            string.Join<string>(Environment.NewLine, actualContents));
    }
}
