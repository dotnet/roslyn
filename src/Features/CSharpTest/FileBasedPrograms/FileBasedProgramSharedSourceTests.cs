// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.FileBasedPrograms;

public sealed class FileBasedProgramSharedSourceTests
{
    /// <summary>
    /// Verifies the shared source files under <c>src/Features/CSharp/Portable/FileBasedPrograms</c>
    /// exactly match the copies shipped in the <c>Microsoft.DotNet.FileBasedPrograms</c> NuGet package
    /// If any file is missing or differs, the local file is regenerated from the package content
    /// and the test fails listing the changes.
    /// <para/>
    /// We do this instead of including the source package directly as <c>PackageReference</c>
    /// because that would not work in source build (which requires roslyn to build before sdk).
    /// </summary>
    [ConditionalFact(typeof(DesktopOnly), Reason = "Avoid regenerating snapshots multiple times")]
    public void Match()
    {
        var root = FindRepoRoot();
        Assert.True(root != null, "Could not locate repo root.");

        var versionDetailsProps = Path.Combine(root, "eng", "Version.Details.props");
        Assert.True(File.Exists(versionDetailsProps), $"'{versionDetailsProps}' not found.");

        var packageVersion = GetPackageVersion(versionDetailsProps, "MicrosoftDotNetFileBasedProgramsPackageVersion");

        var globalPackagesFolder = GetGlobalPackagesFolder();
        Assert.True(Directory.Exists(globalPackagesFolder), $"Global packages folder not found: {globalPackagesFolder}");

        var packageRoot = Path.Combine(globalPackagesFolder, "microsoft.dotnet.filebasedprograms", packageVersion);
        Assert.True(Directory.Exists(packageRoot), $"Package folder not found: {packageRoot}");

        var contentFilesDir1 = Path.Combine(packageRoot, "contentFiles", "cs", "any");
        Assert.True(Directory.Exists(contentFilesDir1), $"contentFiles directory not found: {contentFilesDir1}");

        var contentFilesDir2 = Path.Combine(packageRoot, "contentFiles", "cs", "netstandard2.0");
        Assert.True(Directory.Exists(contentFilesDir2), $"contentFiles directory not found: {contentFilesDir2}");

        var localSourceDir = Path.Combine(root, "src", "Features", "CSharp", "Portable", "FileBasedPrograms");
        Assert.True(Directory.Exists(Path.GetDirectoryName(localSourceDir)), $"Local source directory not found: {localSourceDir}");

        var packageFiles = Directory.GetFiles(contentFilesDir1, "*", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(contentFilesDir2, "*", SearchOption.TopDirectoryOnly))
            .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.NotEmpty(packageFiles);

        Directory.CreateDirectory(localSourceDir);

        var mismatches = new List<string>();
        foreach (var pkgFile in packageFiles)
        {
            var fileName = Path.GetFileName(pkgFile);
            var localFile = Path.Combine(localSourceDir, fileName);
            var pkgContent = File.ReadAllText(pkgFile);

            if (!File.Exists(localFile))
            {
                // Create missing file from package content.
                File.WriteAllText(localFile, pkgContent);
                mismatches.Add($"Added missing file: {fileName}");
                continue;
            }

            var localContent = File.ReadAllText(localFile);
            if (!string.Equals(localContent.NormalizeLineEndings(), pkgContent.NormalizeLineEndings(), StringComparison.Ordinal))
            {
                // Regenerate local file to match package.
                File.WriteAllText(localFile, pkgContent);
                mismatches.Add($"Updated file: {fileName}");
            }
        }

        // If there are extra local files that are expected to mirror package files, report them but do not delete.
        var localMirrorFiles = Directory.GetFiles(localSourceDir, "*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var pkgName in packageFiles.Select(Path.GetFileName))
            localMirrorFiles.Remove(pkgName);
        if (localMirrorFiles.Count > 0)
        {
            mismatches.Add("Extra local files (not in package): " + string.Join(", ", localMirrorFiles));
        }

        if (mismatches.Count > 0)
        {
            Assert.Fail("Shared source for FileBasedPrograms was out of sync with package. Regenerated. Changes: " + string.Join(" | ", mismatches));
        }
    }

    private static string? FindRepoRoot([CallerFilePath] string startPath = ".")
    {
        var dir = Path.GetDirectoryName(startPath);
        while (dir != null && Directory.Exists(dir))
        {
            if (File.Exists(Path.Combine(dir, "Directory.Packages.props")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    private static string GetPackageVersion(string xmlFilePath, string propertyName)
    {
        var doc = XDocument.Load(xmlFilePath);
        // Look for <{propertyName}>{version}</{propertyName}>
        var packageVersionElement = doc.Descendants().FirstOrDefault(e =>
            string.Equals(e.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"'{propertyName}' not found in '{xmlFilePath}'");
        return packageVersionElement.Value;
    }

    private static string GetGlobalPackagesFolder()
    {
        // Respect NUGET_PACKAGES if set; otherwise default location under user profile.
        var envOverride = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!string.IsNullOrWhiteSpace(envOverride))
            return envOverride!;

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile))
            throw new InvalidOperationException("Cannot determine user profile path for global packages folder.");
        return Path.Combine(userProfile, ".nuget", "packages");
    }
}
