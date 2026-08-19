// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.PackageValidation;

/// <summary>
/// Drives a single <see cref="NuGetPackageUpgradeScenario"/> end to end: generates an isolated legacy
/// (packages.config) solution, installs the package's previously released version through the Package
/// Manager Console (PMC), verifies and builds that baseline, upgrades to the locally built candidate
/// version through PMC, and verifies and builds the result.
/// </summary>
/// <remarks>
/// This runner intentionally contains no package-specific special cases beyond what is supplied through
/// <see cref="NuGetPackageUpgradeScenario"/>: scenario metadata (which package, which API to exercise)
/// and scenario-specific asset assertions are the only per-package inputs. Everything else (PMC
/// commands, packages.config/HintPath assertions, and building) is shared so that a second scenario can
/// be added without duplicating this logic.
/// </remarks>
internal sealed class NuGetPackageUpgradeScenarioRunner
{
    private readonly TestServices _testServices;
    private readonly CancellationToken _cancellationToken;
    private readonly NuGetPackageUpgradeScenario _scenario;
    private readonly PackageUpgradeEntry _packageUpgradeEntry;
    private readonly PackageValidationSource _validationSource;

    public NuGetPackageUpgradeScenarioRunner(
        TestServices testServices,
        NuGetPackageUpgradeScenario scenario,
        PackageUpgradeConfiguration packageUpgradeConfiguration,
        PackageValidationSource validationSource,
        CancellationToken cancellationToken)
    {
        _testServices = testServices;
        _scenario = scenario;
        _validationSource = validationSource;
        _cancellationToken = cancellationToken;

        _packageUpgradeEntry = FindPackageUpgradeEntry(packageUpgradeConfiguration, scenario.TopLevelPackageId);
    }

    /// <summary>The generated legacy solution/project, valid once <see cref="RunAsync"/> has created it.</summary>
    public LegacyPackageProject Project { get; private set; } = null!;

    /// <summary>Runs the full baseline-install, verify, build, upgrade, verify, build sequence.</summary>
    public async Task RunAsync()
    {
        Project = LegacyPackageProject.Create(
            _scenario.ProjectName,
            packageSources:
            [
                ("nuget.org", "https://api.nuget.org/v3/index.json"),
                ("RoslynPackageValidation", _validationSource.PackagesDirectory),
            ]);

        try
        {
            await _testServices.SolutionExplorer.OpenSolutionAsync(Project.SolutionFilePath, _cancellationToken);

            await InstallBaselineAsync();
            VerifyBaseline();
            await BuildAndVerifySuccessAsync("baseline");

            await UpgradeToCandidateAsync();
            await VerifyUpgradeAsync();
            await AddApiUsageFileAsync();
            await BuildAndVerifySuccessAsync("upgrade");
        }
        finally
        {
            Project.TryDelete();
        }
    }

    private async Task InstallBaselineAsync()
    {
        var command = $"Install-Package {_scenario.TopLevelPackageId} -Version {_packageUpgradeEntry.PreviousReleaseVersion} -ProjectName {Project.ProjectName} -Source nuget.org";
        var result = await _testServices.PackageManagerConsole.ExecuteCommandAsync(command, _cancellationToken);
        Assert.True(result.Succeeded, $"Baseline package install failed.{Environment.NewLine}{result.GetFailureMessage()}");
    }

    private void VerifyBaseline()
    {
        Assert.True(File.Exists(Project.PackagesConfigPath), $"Expected 'packages.config' to exist after installing '{_scenario.TopLevelPackageId}': '{Project.PackagesConfigPath}'.");

        var packagesConfig = File.ReadAllText(Project.PackagesConfigPath);
        Assert.Contains(
            $"id=\"{_scenario.TopLevelPackageId}\" version=\"{_packageUpgradeEntry.PreviousReleaseVersion}\"",
            packagesConfig,
            StringComparison.Ordinal);

        var projectContent = File.ReadAllText(Project.ProjectFilePath);
        Assert.Contains(_packageUpgradeEntry.PreviousReleaseVersion, projectContent, StringComparison.Ordinal);
        Assert.Contains(Project.PackagesDirectory, projectContent, StringComparison.OrdinalIgnoreCase);
    }

    private async Task UpgradeToCandidateAsync()
    {
        var candidatePackage = _validationSource.GetRequiredPackage(_scenario.TopLevelPackageId);

        var command = $"Update-Package {_scenario.TopLevelPackageId} -Version {candidatePackage.Version} -ProjectName {Project.ProjectName} -Source RoslynPackageValidation";
        var result = await _testServices.PackageManagerConsole.ExecuteCommandAsync(command, _cancellationToken);
        Assert.True(result.Succeeded, $"Candidate package update failed.{Environment.NewLine}{result.GetFailureMessage()}");
    }

    private async Task VerifyUpgradeAsync()
    {
        await _testServices.SolutionExplorer.SaveAllAsync(_cancellationToken);

        var candidateVersion = _validationSource.CandidateVersion;

        var packagesConfig = File.ReadAllText(Project.PackagesConfigPath);
        Assert.Contains(
            $"id=\"{_scenario.TopLevelPackageId}\" version=\"{candidateVersion}\"",
            packagesConfig,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            $"version=\"{_packageUpgradeEntry.PreviousReleaseVersion}\"",
            packagesConfig,
            StringComparison.Ordinal);

        var projectContent = File.ReadAllText(Project.ProjectFilePath);
        Assert.DoesNotContain(_packageUpgradeEntry.PreviousReleaseVersion, projectContent, StringComparison.Ordinal);

        if (_scenario.VerifyAdditionalUpgradeAssetsAsync is not null)
        {
            await _scenario.VerifyAdditionalUpgradeAssetsAsync(this, _cancellationToken);
        }
    }

    /// <summary>
    /// The isolated packages directory for the upgraded (candidate version) package, e.g. to inspect
    /// package-specific assets which are not represented as project references (such as content files).
    /// </summary>
    public string GetInstalledCandidatePackageDirectory()
    {
        var candidatePackage = _validationSource.GetRequiredPackage(_scenario.TopLevelPackageId);
        return Path.Combine(Project.PackagesDirectory, $"{_scenario.TopLevelPackageId}.{candidatePackage.Version}");
    }

    private async Task AddApiUsageFileAsync()
    {
        // Added only after the upgrade so the API it exercises (from the candidate package) is
        // guaranteed to be available; the baseline build above only needs to succeed with the
        // previously released package, not compile candidate-only APIs.
        await _testServices.SolutionExplorer.AddFileAsync(
            Project.ProjectName,
            _scenario.ApiUsageFileName,
            _scenario.CreateApiUsageSource(),
            open: false,
            cancellationToken: _cancellationToken);
        await _testServices.SolutionExplorer.SaveAllAsync(_cancellationToken);
    }

    private async Task BuildAndVerifySuccessAsync(string phase)
    {
        var succeeded = await _testServices.SolutionExplorer.BuildSolutionAndWaitAsync(_cancellationToken);

        var errors = succeeded ? ImmutableArray<string>.Empty : await _testServices.ErrorList.GetBuildErrorsAsync(_cancellationToken);
        Assert.True(succeeded, $"Build failed after {phase} for '{_scenario.TopLevelPackageId}':{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }

    private static PackageUpgradeEntry FindPackageUpgradeEntry(PackageUpgradeConfiguration configuration, string packageId)
    {
        foreach (var entry in configuration.Packages)
        {
            if (string.Equals(entry.Id, packageId, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        throw new InvalidOperationException($"'{packageId}' is not listed in 'eng/config/NuGetPackageUpgradeValidation.json'.");
    }
}
