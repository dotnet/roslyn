// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.RoslynTools.NuGet;

internal class NuGetPrepare
{
    private const string NotPublishedDirectoryName = "NotPublished";

    private static readonly string[] RoslynPackageIds =
        [
            "Microsoft.CodeAnalysis",
            "Microsoft.CodeAnalysis.Common",
            "Microsoft.CodeAnalysis.Compilers",
            "Microsoft.CodeAnalysis.CSharp",
            "Microsoft.CodeAnalysis.CSharp.CodeStyle",
            "Microsoft.CodeAnalysis.CSharp.Features",
            "Microsoft.CodeAnalysis.CSharp.Scripting",
            "Microsoft.CodeAnalysis.CSharp.Workspaces",
            "Microsoft.CodeAnalysis.EditorFeatures.Text",
            "Microsoft.CodeAnalysis.Features",
            "Microsoft.CodeAnalysis.Scripting",
            "Microsoft.CodeAnalysis.Scripting.Common",
            "Microsoft.CodeAnalysis.VisualBasic",
            "Microsoft.CodeAnalysis.VisualBasic.CodeStyle",
            "Microsoft.CodeAnalysis.VisualBasic.Features",
            "Microsoft.CodeAnalysis.VisualBasic.Workspaces",
            "Microsoft.CodeAnalysis.Workspaces.Common",
            "Microsoft.CodeAnalysis.Workspaces.MSBuild",
            "Microsoft.Net.Compilers.Toolset",
            "Microsoft.Net.Compilers.Toolset.Framework",
            "Microsoft.VisualStudio.LanguageServices"
        ];

    internal static async Task<int> PrepareAsync(ILogger logger)
    {
        try
        {
            string? version;

            var determinedVersion = TryDetermineRoslynPackageVersion(out version);

            if (!determinedVersion)
            {
                logger.LogError("Expected packages are missing. Unable to determine version.");
                return 1;
            }

            logger.LogInformation($"Moving {version} packages...");

            var publishedPackages = RoslynPackageIds
                .Select(packageId => $"{packageId}.{version}.nupkg")
                .ToHashSet();

            if (!Directory.Exists(NotPublishedDirectoryName))
            {
                Directory.CreateDirectory(NotPublishedDirectoryName);
            }

            var packageFolder = Environment.CurrentDirectory;
            var packageFiles = Directory.GetFiles(packageFolder, "*.nupkg");

            foreach (var packageFile in packageFiles)
            {
                if (IsPublishedPackage(packageFile, publishedPackages))
                {
                    continue;
                }

                MovePackageAsync(packageFile);
            }

            logger.LogInformation("Packages moved.");

            await NuGetDependencyFinder.FindDependenciesAsync(packageFolder, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return 1;
        }

        return 0;

        static bool TryDetermineRoslynPackageVersion([NotNullWhen(returnValue: true)] out string? version)
        {
            var packageFileName = Directory.GetFiles(Environment.CurrentDirectory, "Microsoft.Net.Compilers.Toolset.*.nupkg").FirstOrDefault();
            if (packageFileName is null)
            {
                version = null;
                return false;
            }

            version = Path.GetFileNameWithoutExtension(packageFileName).Substring(32);
            return true;
        }

        static bool IsPublishedPackage(string packagePath, HashSet<string> publishedPackages)
        {
            var packageFileName = Path.GetFileName(packagePath);
            return publishedPackages.Contains(packageFileName);
        }

        static void MovePackageAsync(string packagePath)
        {
            var packageFileName = Path.GetFileName(packagePath);
            File.Move(packagePath, Path.Combine(NotPublishedDirectoryName, packageFileName));
        }
    }
}
