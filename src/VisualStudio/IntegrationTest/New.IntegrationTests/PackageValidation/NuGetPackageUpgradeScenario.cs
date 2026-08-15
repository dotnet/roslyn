// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.VisualStudio.NewIntegrationTests.PackageValidation;

/// <summary>
/// Describes a single legacy packages.config upgrade scenario driven by <see cref="NuGetPackageUpgradeScenarioRunner"/>.
/// Each scenario upgrades exactly one top-level Roslyn package in its own isolated solution, so a
/// dependency problem surfaced by one scenario cannot be masked by NuGet reference unification with
/// another scenario's packages.
/// </summary>
/// <param name="ProjectName">
/// The name given to the generated solution/project. Must be unique across scenarios so their scratch
/// directories, packages directories, and NuGet caches never overlap.
/// </param>
/// <param name="TopLevelPackageId">
/// The Roslyn NuGet package id being validated, e.g. <c>Microsoft.CodeAnalysis</c>. Its previously
/// released version is looked up from <c>eng/config/NuGetPackageUpgradeValidation.json</c>, and its
/// locally built candidate version is looked up from the NuGet package upgrade validation payload.
/// </param>
/// <param name="ApiUsageFileName">The file name given to the generated API-usage source file.</param>
/// <param name="CreateApiUsageSource">
/// Produces C#/VB source which exercises the package's public API, so that the compiled candidate
/// package (rather than only its packages.config/HintPath entries) is verified to actually work. This
/// file is only added to the project *after* the baseline install, since the API it exercises does not
/// exist until the package's reference assemblies are present.
/// </param>
/// <param name="VerifyAdditionalUpgradeAssetsAsync">
/// Optional additional verification specific to this scenario's package (for example, confirming a
/// net472-only asset was staged by NuGet). Runs after the standard upgrade verification.
/// </param>
internal sealed record NuGetPackageUpgradeScenario(
    string ProjectName,
    string TopLevelPackageId,
    string ApiUsageFileName,
    Func<string> CreateApiUsageSource,
    Func<NuGetPackageUpgradeScenarioRunner, CancellationToken, Task>? VerifyAdditionalUpgradeAssetsAsync = null);
