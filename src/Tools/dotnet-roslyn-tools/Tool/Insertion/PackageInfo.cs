// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using NuGet.Versioning;

namespace Microsoft.RoslynTools.Insertion;

internal struct PackageInfo
{
    private const string PackageNamePrefix = "VS.ExternalAPIs.";
    private const string PackageExtension = ".nupkg";

    public const string RoslynToolsetPackageName = "VS.Tools.Roslyn";

    /// <summary>
    /// Name of the CoreXT package, e.g. VS.ExternalAPI.Roslyn, Microsoft.DiaSymReader
    /// </summary>
    public readonly string PackageName;

    /// <summary>
    /// Library name, e.g. Roslyn, Microsoft.DiaSymReader.
    /// </summary>
    public readonly string LibraryName;

    /// <summary>
    /// Version, e.g. 1.3.0-beta1-20160315-05
    /// </summary>
    public readonly NuGetVersion Version;

    public readonly bool IsRoslyn => LibraryName == "Roslyn";

    public readonly bool IsRoslynToolsetCompiler => PackageName == RoslynToolsetPackageName;

    public PackageInfo(string packageName, string libraryName, NuGetVersion version)
    {
        PackageName = packageName;
        LibraryName = libraryName;
        Version = version;
    }

    public override readonly string ToString() => $"{PackageName}.{Version}";

    public static PackageInfo ParsePackageFileName(string fileName)
    {
        if (!fileName.EndsWith(PackageExtension))
        {
            throw new InvalidDataException($"Invalid package name: '{fileName}'");
        }

        var libraryNameStartIndex = fileName.StartsWith(PackageNamePrefix) ? PackageNamePrefix.Length : 0;

        var parts = fileName.Substring(libraryNameStartIndex, fileName.Length - libraryNameStartIndex - PackageExtension.Length).Split('.');
        var firstNumber = IndexOfNumericPart(parts);
        if (firstNumber == -1)
        {
            throw new InvalidDataException($"Invalid package name: '{fileName}'");
        }

        var libraryName = string.Join(".", parts.Take(firstNumber));
        var packageName = string.Concat(fileName.AsSpan(0, libraryNameStartIndex), libraryName);
        var versionStr = string.Join(".", parts.Skip(firstNumber));

        if (!NuGetVersion.TryParse(versionStr, out var version))
        {
            throw new InvalidDataException($"Invalid version number: '{fileName}'");
        }

        return new PackageInfo(packageName, libraryName, version);
    }

    private static int IndexOfNumericPart(string[] parts)
    {
        for (var i = 0; i < parts.Length; i++)
        {
            if (int.TryParse(parts[i], out var _))
            {
                return i;
            }
        }

        return -1;
    }
}
