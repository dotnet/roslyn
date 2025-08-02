// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

public class CSharpRedirectFeaturesAnalyzers : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    private const int GlobalIndentationSize = 6;
    private const int DefaultIndentationSize = 4;

    [IdeFact]
    public async Task DoesNotUseHostOptions_WhenEnforceCodeStyleInBuildIsTrue()
    {
        await SetupSolutionAsync(
            enforceCodeStyleInBuild: true,
            GlobalIndentationSize,
            HangMitigatingCancellationToken);

        var code = GenerateTestCode(GlobalIndentationSize);

        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "C.cs", code, open: true, cancellationToken: HangMitigatingCancellationToken);

        var errors = await GetErrorsFromErrorListAsync(HangMitigatingCancellationToken);
        AssertEx.Equal(
            [
                "C.cs(3, 5): error IDE0055: Fix formatting",
                "C.cs(4, 5): error IDE0055: Fix formatting",
                "C.cs(6, 5): error IDE0055: Fix formatting",
            ],
            errors);
    }

    [IdeFact]
    public async Task UsesHostOptions_WhenEnforceCodeStyleInBuildIsFalse()
    {
        await SetupSolutionAsync(
            enforceCodeStyleInBuild: false,
            GlobalIndentationSize,
            HangMitigatingCancellationToken);

        var code = GenerateTestCode(DefaultIndentationSize);

        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "C.cs", code, open: true, cancellationToken: HangMitigatingCancellationToken);

        var errors = await GetErrorsFromErrorListAsync(HangMitigatingCancellationToken);
        AssertEx.Equal(
            [
                "C.cs(3, 5): error IDE0055: Fix formatting",
                "C.cs(4, 5): error IDE0055: Fix formatting",
                "C.cs(6, 5): error IDE0055: Fix formatting",
            ],
            errors);
    }

    private async Task SetupSolutionAsync(bool enforceCodeStyleInBuild, int globalIndentationSize, CancellationToken cancellationToken)
    {
        await TestServices.SolutionExplorer.CreateSolutionAsync(SolutionName, cancellationToken);

        await TestServices.SolutionExplorer.AddCustomProjectAsync(
            ProjectName,
            ".csproj",
            $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <Nullable>enable</Nullable>
                <EnforceCodeStyleInBuild>{enforceCodeStyleInBuild}</EnforceCodeStyleInBuild>
              </PropertyGroup>

            </Project>
            """,
            cancellationToken);

        // Configure the global indentation size which would be part of the Host fallback options.
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
        globalOptions.SetGlobalOption(FormattingOptions2.UseTabs, LanguageNames.CSharp, false);
        globalOptions.SetGlobalOption(FormattingOptions2.IndentationSize, LanguageNames.CSharp, globalIndentationSize);

        // Add .editorconfig to configure so that formatting diagnostics are errors
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName,
            ".editorconfig",
            """
            root = true

            [*.cs]
            dotnet_diagnostic.IDE0055.severity = error
            """,
            open: false,
            cancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles
            ],
            cancellationToken);
    }

    private static string GenerateTestCode(int indentationSize)
    {
        var indentation = new string(' ', indentationSize);
        return $$"""
            class C
            {
            {{indentation}}void M()
            {{indentation}}{

            {{indentation}}}
            }
            """;
    }

    private async Task<ImmutableArray<string>> GetErrorsFromErrorListAsync(CancellationToken cancellationToken)
    {
        await WaitForCodeActionListToPopulateAsync(cancellationToken);

        await TestServices.ErrorList.ShowErrorListAsync(cancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles,
                FeatureAttribute.ErrorList
             ],
             cancellationToken);
        return await TestServices.ErrorList.GetErrorsAsync(ErrorSource.Other, __VSERRORCATEGORY.EC_WARNING, cancellationToken);
    }

    private async Task WaitForCodeActionListToPopulateAsync(CancellationToken cancellationToken)
    {
        await TestServices.Editor.ActivateAsync(cancellationToken);
        await TestServices.Editor.PlaceCaretAsync("void M()", charsOffset: -1, cancellationToken);

        await Task.Delay(TimeSpan.FromSeconds(1));

        await TestServices.Editor.InvokeCodeActionListAsync(cancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles
            ],
            cancellationToken);

        await TestServices.Editor.DismissLightBulbSessionAsync(cancellationToken);
    }
}
