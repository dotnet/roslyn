#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Linq;

// Verifies the shared source files under `src/Features/CSharp/Portable/FileBasedPrograms`
// exactly match the copies shipped in the `Microsoft.DotNet.FileBasedPrograms` NuGet package
// If any file is missing or differs, the local file is regenerated from the package content
// and the test fails listing the changes.
//
// We do this instead of including the source package directly as `PackageReference`
// because that would not work in source build (which requires roslyn to build before sdk).

var root = Path.Join(AppContext.GetData("EntryPointFileDirectoryPath") as string, "..");
if (!Directory.Exists(root)) throw new InvalidOperationException($"Could not locate repo root: {root}");

var versionDetailsProps = Path.Combine(root, "eng", "Version.Details.props");
if (!File.Exists(versionDetailsProps)) throw new InvalidOperationException($"'{versionDetailsProps}' not found.");

var packageVersion = GetPackageVersion(versionDetailsProps, "MicrosoftDotNetFileBasedProgramsPackageVersion");

var globalPackagesFolder = GetGlobalPackagesFolder();
if (!Directory.Exists(globalPackagesFolder)) throw new InvalidOperationException($"Global packages folder not found: {globalPackagesFolder}");

var packageRoot = Path.Combine(globalPackagesFolder, "microsoft.dotnet.filebasedprograms", packageVersion);
if (!Directory.Exists(packageRoot)) throw new InvalidOperationException($"Package folder not found: {packageRoot}");

var contentFilesDir1 = Path.Combine(packageRoot, "contentFiles", "cs", "any");
if (!Directory.Exists(contentFilesDir1)) throw new InvalidOperationException($"contentFiles directory not found: {contentFilesDir1}");

var contentFilesDir2 = Path.Combine(packageRoot, "contentFiles", "cs", "netstandard2.0");
if (!Directory.Exists(contentFilesDir2)) throw new InvalidOperationException($"contentFiles directory not found: {contentFilesDir2}");

var localSourceDir = Path.Combine(root, "src", "Features", "CSharp", "Portable", "FileBasedPrograms");
if (!Directory.Exists(Path.GetDirectoryName(localSourceDir))) throw new InvalidOperationException($"Local source directory not found: {localSourceDir}");

var extensions = new[] { ".cs", ".resx", ".editorconfig" };

var packageFiles = Directory.GetFiles(contentFilesDir1, "*", SearchOption.TopDirectoryOnly)
    .Concat(Directory.GetFiles(contentFilesDir2, "*", SearchOption.TopDirectoryOnly))
    .Where(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
    .ToList();
if (packageFiles.Count == 0) throw new InvalidOperationException("No package files found.");

var updateSnapshots = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));

if (updateSnapshots) Directory.CreateDirectory(localSourceDir);

var mismatches = new List<string>();
foreach (var pkgFile in packageFiles)
{
    var fileName = Path.GetFileName(pkgFile);
    var localFile = Path.Combine(localSourceDir, fileName);
    var pkgContent = File.ReadAllText(pkgFile);

    if (!File.Exists(localFile))
    {
        // Create missing file from package content.
        if (updateSnapshots) File.WriteAllText(localFile, pkgContent);
        mismatches.Add($"Added missing file: {fileName}");
        continue;
    }

    var localContent = File.ReadAllText(localFile);
    if (!string.Equals(localContent.ReplaceLineEndings(), pkgContent.ReplaceLineEndings(), StringComparison.Ordinal))
    {
        // Regenerate local file to match package.
        if (updateSnapshots) File.WriteAllText(localFile, pkgContent);
        mismatches.Add($"Updated file: {fileName}");
    }
}

// If there are extra local files that are expected to mirror package files, report them but do not delete.
var localMirrorFiles = Directory.GetFiles(localSourceDir, "*", SearchOption.TopDirectoryOnly)
    .Where(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
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
    var action = updateSnapshots ? "Regenerated" : "Not regenerated in CI";
    throw new InvalidOperationException($"Shared source for FileBasedPrograms is out of sync with package. {action}. Changes:\n" + string.Join("\n", mismatches));
}

Console.WriteLine("OK");

static string GetPackageVersion(string xmlFilePath, string propertyName)
{
    var doc = XDocument.Load(xmlFilePath);
    // Look for <{propertyName}>{version}</{propertyName}>
    var packageVersionElement = doc.Descendants().FirstOrDefault(e =>
        string.Equals(e.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"'{propertyName}' not found in '{xmlFilePath}'");
    return packageVersionElement.Value;
}

static string GetGlobalPackagesFolder()
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
