// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;

namespace Roslyn.VisualStudio.NewIntegrationTests.PackageValidation;

/// <summary>
/// A locally built Roslyn NuGet package staged by <c>eng/prepare-nuget-package-upgrade-payload.ps1</c>.
/// </summary>
/// <param name="Id">The NuGet package id.</param>
/// <param name="Version">The candidate version built for this validation run.</param>
/// <param name="PackagePath">The absolute path to the staged <c>.nupkg</c> file.</param>
internal sealed record PackageValidationPackage(string Id, string Version, string PackagePath);

/// <summary>
/// Reads the NuGet package upgrade validation payload prepared by <c>eng/prepare-nuget-package-upgrade-payload.ps1</c>
/// and referenced by the <c>ROSLYN_PACKAGE_VALIDATION_SOURCE</c> environment variable. The payload directory
/// contains a <c>package-manifest.json</c> describing the candidate version and a <c>packages</c> subdirectory
/// which doubles as a local NuGet flat-folder package source.
/// </summary>
internal sealed class PackageValidationSource
{
    /// <summary>
    /// Environment variable naming the directory produced by <c>eng/prepare-nuget-package-upgrade-payload.ps1</c>.
    /// Set this to a local directory containing <c>package-manifest.json</c> to run the NuGet package upgrade
    /// validation tests (<c>TestGate=NuGetPackageUpgrade</c>) manually.
    /// </summary>
    public const string EnvironmentVariableName = "ROSLYN_PACKAGE_VALIDATION_SOURCE";

    private readonly ImmutableDictionary<string, PackageValidationPackage> _packagesById;

    private PackageValidationSource(string packagesDirectory, string candidateVersion, ImmutableDictionary<string, PackageValidationPackage> packagesById)
    {
        PackagesDirectory = packagesDirectory;
        CandidateVersion = candidateVersion;
        _packagesById = packagesById;
    }

    /// <summary>
    /// The directory containing the staged <c>.nupkg</c> files. Usable directly as a NuGet flat-folder
    /// package source.
    /// </summary>
    public string PackagesDirectory { get; }

    /// <summary>The candidate Roslyn package version built for this validation run.</summary>
    public string CandidateVersion { get; }

    /// <summary>
    /// Loads the validation payload referenced by the <see cref="EnvironmentVariableName"/> environment
    /// variable.
    /// </summary>
    public static PackageValidationSource LoadFromEnvironment()
    {
        var root = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException(
                $"""
                The '{EnvironmentVariableName}' environment variable is not set.
                Set it to a directory produced by 'eng/prepare-nuget-package-upgrade-payload.ps1'
                (containing 'package-manifest.json') to run this test manually.
                """);
        }

        return LoadFromDirectory(root);
    }

    /// <summary>Loads the validation payload from an explicit directory, primarily for testing.</summary>
    public static PackageValidationSource LoadFromDirectory(string rootDirectory)
    {
        var manifestPath = Path.Combine(rootDirectory, "package-manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException(
                $"NuGet package upgrade validation manifest was not found: '{manifestPath}'. Run 'eng/prepare-nuget-package-upgrade-payload.ps1' first.",
                manifestPath);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        if (!root.TryGetProperty("candidateVersion", out var candidateVersionElement) || candidateVersionElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"The NuGet package upgrade validation manifest '{manifestPath}' is missing a 'candidateVersion' string.");
        }

        var candidateVersion = candidateVersionElement.GetString()!;

        if (!root.TryGetProperty("packages", out var packagesElement) || packagesElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"The NuGet package upgrade validation manifest '{manifestPath}' is missing a 'packages' array.");
        }

        var packagesDirectory = Path.Combine(rootDirectory, "packages");
        var builder = ImmutableDictionary.CreateBuilder<string, PackageValidationPackage>(StringComparer.OrdinalIgnoreCase);

        foreach (var packageElement in packagesElement.EnumerateArray())
        {
            var id = GetRequiredString(packageElement, "id", manifestPath);
            var version = GetRequiredString(packageElement, "version", manifestPath);
            var relativePath = GetRequiredString(packageElement, "path", manifestPath);

            var packagePath = Path.Combine(rootDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            builder[id] = new PackageValidationPackage(id, version, packagePath);
        }

        return new PackageValidationSource(packagesDirectory, candidateVersion, builder.ToImmutable());
    }

    /// <summary>
    /// Gets the staged candidate package for <paramref name="id"/>, throwing if it was not found. This
    /// also asserts the staged version matches <see cref="CandidateVersion"/>, since the payload
    /// preparation script guarantees exactly one version per package id.
    /// </summary>
    public PackageValidationPackage GetRequiredPackage(string id)
    {
        if (!_packagesById.TryGetValue(id, out var package))
        {
            throw new InvalidOperationException($"NuGet package upgrade validation payload does not contain package '{id}'. Available packages: {string.Join(", ", GetAvailablePackageIds())}.");
        }

        if (!string.Equals(package.Version, CandidateVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"NuGet package upgrade validation payload package '{id}' has version '{package.Version}' which does not match candidate version '{CandidateVersion}'.");
        }

        return package;
    }

    private IEnumerable<string> GetAvailablePackageIds() => _packagesById.Keys;

    private static string GetRequiredString(JsonElement element, string propertyName, string manifestPath)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Each package in the NuGet package upgrade validation manifest '{manifestPath}' must specify a '{propertyName}' string.");
        }

        var text = value.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException($"The '{propertyName}' value in the NuGet package upgrade validation manifest '{manifestPath}' must not be empty.");
        }

        return text!;
    }
}
