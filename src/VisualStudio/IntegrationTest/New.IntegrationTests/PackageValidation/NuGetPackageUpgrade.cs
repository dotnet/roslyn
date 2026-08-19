// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.PackageValidation;

/// <summary>
/// Validates upgrading Roslyn NuGet packages in legacy (packages.config) projects through the Package
/// Manager Console (PMC), end to end: install the previously released version, verify and build that
/// baseline, upgrade to the locally built candidate version via <c>Update-Package</c>, and verify and
/// build source that consumes the upgraded package's public API.
/// </summary>
/// <remarks>
/// <para>
/// These tests require a local NuGet package upgrade validation payload (see
/// <c>eng/prepare-nuget-package-upgrade-payload.ps1</c>) referenced by the
/// <see cref="PackageValidationSource.EnvironmentVariableName"/> environment variable, plus real network
/// access to install the previously released package from nuget.org. They are tagged
/// <c>TestGate=NuGetPackageUpgrade</c> so ordinary integration and DartLab runs (which filter on
/// <c>TestGate!=NuGetPackageUpgrade</c> or <c>TestGate=RoslynVSIntegration</c>) never select them; only a
/// dedicated pipeline job which opts in with <c>TestGate=NuGetPackageUpgrade</c> runs them.
/// </para>
/// <para>
/// Each scenario below uses its own isolated solution and packages directory (see
/// <see cref="NuGetPackageUpgradeScenarioRunner"/>). Do not upgrade more than one top-level package in a
/// single solution: NuGet's dependency unification could then let one scenario's correct resolution mask
/// a real dependency problem which would otherwise only affect the other scenario's package.
/// </para>
/// </remarks>
[Trait(Traits.TestGate, Traits.TestGates.NuGetPackageUpgrade)]
public class NuGetPackageUpgrade : AbstractIntegrationTest
{
    private static PackageUpgradeConfiguration LoadPackageUpgradeConfiguration()
        => PackageUpgradeConfiguration.LoadFromFile(
            Path.Combine(GetRepositoryRoot(), "eng", "config", "NuGetPackageUpgradeValidation.json"));

    /// <summary>
    /// Walks up from the running test assembly's directory to find the repository root, identified by
    /// the presence of <c>eng/config/NuGetPackageUpgradeValidation.json</c>.
    /// </summary>
    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "eng", "config", "NuGetPackageUpgradeValidation.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not locate the repository root (containing 'eng/config/NuGetPackageUpgradeValidation.json') from '{AppContext.BaseDirectory}'.");
    }

    /// <summary>
    /// Validates upgrading the <c>Microsoft.CodeAnalysis</c> package, which brings in both the C# and VB
    /// compiler APIs.
    /// </summary>
    [IdeFact]
    public async Task UpgradeMicrosoftCodeAnalysisAsync()
    {
        var validationSource = PackageValidationSource.LoadFromEnvironment();
        var packageUpgradeConfiguration = LoadPackageUpgradeConfiguration();

        var scenario = new NuGetPackageUpgradeScenario(
            ProjectName: "CodeAnalysisPackageUpgrade",
            TopLevelPackageId: "Microsoft.CodeAnalysis",
            ApiUsageFileName: "CompilerApiUsage.cs",
            CreateApiUsageSource: static () => """
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.CSharp;
                using Microsoft.CodeAnalysis.VisualBasic;

                namespace CodeAnalysisPackageUpgrade
                {
                    public class CompilerApiUsage
                    {
                        public SyntaxTree ParseCSharp(string text)
                            => CSharpSyntaxTree.ParseText(text);

                        public SyntaxTree ParseVisualBasic(string text)
                            => VisualBasicSyntaxTree.ParseText(text);
                    }
                }
                """);

        var runner = new NuGetPackageUpgradeScenarioRunner(TestServices, scenario, packageUpgradeConfiguration, validationSource, HangMitigatingCancellationToken);
        await runner.RunAsync();
    }

    /// <summary>
    /// Validates upgrading the <c>Microsoft.CodeAnalysis.Workspaces.MSBuild</c> package, which has a more
    /// complex asset layout (a net472-specific out-of-process build host deployed as package content) and
    /// a larger dependency closure than <c>Microsoft.CodeAnalysis</c>.
    /// </summary>
    [IdeFact]
    public async Task UpgradeMicrosoftCodeAnalysisWorkspacesMSBuildAsync()
    {
        var validationSource = PackageValidationSource.LoadFromEnvironment();
        var packageUpgradeConfiguration = LoadPackageUpgradeConfiguration();

        var scenario = new NuGetPackageUpgradeScenario(
            ProjectName: "MSBuildWorkspacePackageUpgrade",
            TopLevelPackageId: "Microsoft.CodeAnalysis.Workspaces.MSBuild",
            ApiUsageFileName: "MSBuildWorkspaceApiUsage.cs",
            CreateApiUsageSource: static () => """
                using Microsoft.CodeAnalysis.MSBuild;

                namespace MSBuildWorkspacePackageUpgrade
                {
                    public class MSBuildWorkspaceApiUsage
                    {
                        public MSBuildWorkspace Create()
                            => MSBuildWorkspace.Create();
                    }
                }
                """,
            VerifyAdditionalUpgradeAssetsAsync: static (runner, cancellationToken) =>
            {
                var buildHostContentDirectory = Path.Combine(
                    runner.GetInstalledCandidatePackageDirectory(),
                    "contentFiles", "any", "any", "BuildHost-net472");

                Assert.True(
                    Directory.Exists(buildHostContentDirectory) && Directory.EnumerateFiles(buildHostContentDirectory).Any(),
                    $"Expected the net472 out-of-process build host content to be staged under '{buildHostContentDirectory}'.");

                return Task.CompletedTask;
            });

        var runner = new NuGetPackageUpgradeScenarioRunner(TestServices, scenario, packageUpgradeConfiguration, validationSource, HangMitigatingCancellationToken);
        await runner.RunAsync();
    }
}
