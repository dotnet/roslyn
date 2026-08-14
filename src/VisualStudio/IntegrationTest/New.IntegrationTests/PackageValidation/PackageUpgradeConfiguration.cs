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
/// A single package validated by the legacy packages.config upgrade test.
/// </summary>
/// <param name="Id">The NuGet package id.</param>
/// <param name="PreviousReleaseVersion">The last stable version published to nuget.org, which is installed before upgrading.</param>
internal sealed record PackageUpgradeEntry(string Id, string PreviousReleaseVersion);

/// <summary>
/// Parses <c>eng/config/NuGetPackageUpgradeValidation.json</c>, which lists the Roslyn packages the
/// legacy packages.config upgrade test installs at their previously released versions before
/// upgrading them to the locally built packages.
/// </summary>
internal sealed class PackageUpgradeConfiguration
{
    private PackageUpgradeConfiguration(ImmutableArray<PackageUpgradeEntry> packages)
        => Packages = packages;

    public ImmutableArray<PackageUpgradeEntry> Packages { get; }

    public static PackageUpgradeConfiguration LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"NuGet package upgrade validation configuration was not found: '{path}'.", path);
        }

        return Parse(File.ReadAllText(path));
    }

    public static PackageUpgradeConfiguration Parse(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("packages", out var packagesElement)
            || packagesElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("The NuGet package upgrade validation configuration must contain a 'packages' array.");
        }

        var builder = ImmutableArray.CreateBuilder<PackageUpgradeEntry>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var packageElement in packagesElement.EnumerateArray())
        {
            var id = GetRequiredString(packageElement, "id");
            var previousReleaseVersion = GetRequiredString(packageElement, "previousReleaseVersion");

            if (!seenIds.Add(id))
            {
                throw new InvalidOperationException($"The NuGet package upgrade validation configuration lists '{id}' more than once.");
            }

            builder.Add(new PackageUpgradeEntry(id, previousReleaseVersion));
        }

        if (builder.Count == 0)
        {
            throw new InvalidOperationException("The NuGet package upgrade validation configuration must list at least one package.");
        }

        return new PackageUpgradeConfiguration(builder.ToImmutable());
    }

    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Each package in the NuGet package upgrade validation configuration must specify a '{propertyName}' string.");
        }

        var text = value.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException($"The '{propertyName}' value in the NuGet package upgrade validation configuration must not be empty.");
        }

        return text!;
    }
}
