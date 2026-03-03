// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Utilities;

namespace Microsoft.RoslynTools.NuGet;

internal class NuGetPublish
{
    public const string RoslynRepo = "roslyn";
    public const string RoslynSdkRepo = "roslyn-sdk";

    internal static readonly string[] RoslynPackageIds =
    [
        "Microsoft.CodeAnalysis",
        "Microsoft.CodeAnalysis.Analyzers",
        "Microsoft.CodeAnalysis.Common",
        "Microsoft.CodeAnalysis.Compilers",
        "Microsoft.CodeAnalysis.CSharp",
        "Microsoft.CodeAnalysis.CSharp.CodeStyle",
        "Microsoft.CodeAnalysis.CSharp.Features",
        "Microsoft.CodeAnalysis.CSharp.Scripting",
        "Microsoft.CodeAnalysis.CSharp.Workspaces",
        "Microsoft.CodeAnalysis.Features",
        "Microsoft.CodeAnalysis.Scripting",
        "Microsoft.CodeAnalysis.Scripting.Common",
        "Microsoft.CodeAnalysis.VisualBasic",
        "Microsoft.CodeAnalysis.VisualBasic.CodeStyle",
        "Microsoft.CodeAnalysis.VisualBasic.Features",
        "Microsoft.CodeAnalysis.VisualBasic.Workspaces",
        "Microsoft.CodeAnalysis.Workspaces.Common",
        "Microsoft.CodeAnalysis.Workspaces.MSBuild",
        "Microsoft.CodeAnalysis.Workspaces.MSBuild.Contracts",
        "Microsoft.Net.Compilers.Toolset",
        "Microsoft.Net.Compilers.Toolset.Framework",

        // These rely on Visual Studio packages and should not be published at this time.
        //"Microsoft.CodeAnalysis.EditorFeatures.Text",
        //"Microsoft.VisualStudio.LanguageServices",

        // These are the RoslynAnalyzer packages. We should not publish them at this time.
        //"Microsoft.CodeAnalysis.AnalyzerUtilities",
        //"Microsoft.CodeAnalysis.BannedApiAnalyzers",
        //"Microsoft.CodeAnalysis.Metrics",
        //"Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers",
        //"Microsoft.CodeAnalysis.PublicApiAnalyzers",
        //"Microsoft.CodeAnalysis.ResxSourceGenerator",
        //"Microsoft.CodeAnalysis.RulesetToEditorconfigConverter",
        //"Roslyn.Diagnostics.Analyzers",
        //"Text.Analyzers"
    ];

    internal static readonly string[] RoslynSdkPackageIds =
    [
        "Microsoft.CodeAnalysis.Analyzer.Testing",
        "Microsoft.CodeAnalysis.CodeFix.Testing",
        "Microsoft.CodeAnalysis.CodeRefactoring.Testing",
        "Microsoft.CodeAnalysis.CSharp.Analyzer.Testing",
        "Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.MSTest",
        "Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.NUnit",
        "Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit",
        "Microsoft.CodeAnalysis.CSharp.CodeFix.Testing",
        "Microsoft.CodeAnalysis.CSharp.CodeFix.Testing.MSTest",
        "Microsoft.CodeAnalysis.CSharp.CodeFix.Testing.NUnit",
        "Microsoft.CodeAnalysis.CSharp.CodeFix.Testing.XUnit",
        "Microsoft.CodeAnalysis.CSharp.CodeRefactoring.Testing",
        "Microsoft.CodeAnalysis.CSharp.CodeRefactoring.Testing.MSTest",
        "Microsoft.CodeAnalysis.CSharp.CodeRefactoring.Testing.NUnit",
        "Microsoft.CodeAnalysis.CSharp.CodeRefactoring.Testing.XUnit",
        "Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing",
        "Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.MSTest",
        "Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.NUnit",
        "Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit",
        "Microsoft.CodeAnalysis.SourceGenerators.Testing",
        "Microsoft.CodeAnalysis.Testing.Verifiers.MSTest",
        "Microsoft.CodeAnalysis.Testing.Verifiers.NUnit",
        "Microsoft.CodeAnalysis.Testing.Verifiers.XUnit",
        "Microsoft.CodeAnalysis.VisualBasic.Analyzer.Testing",
        "Microsoft.CodeAnalysis.VisualBasic.Analyzer.Testing.MSTest",
        "Microsoft.CodeAnalysis.VisualBasic.Analyzer.Testing.NUnit",
        "Microsoft.CodeAnalysis.VisualBasic.Analyzer.Testing.XUnit",
        "Microsoft.CodeAnalysis.VisualBasic.CodeFix.Testing",
        "Microsoft.CodeAnalysis.VisualBasic.CodeFix.Testing.MSTest",
        "Microsoft.CodeAnalysis.VisualBasic.CodeFix.Testing.NUnit",
        "Microsoft.CodeAnalysis.VisualBasic.CodeFix.Testing.XUnit",
        "Microsoft.CodeAnalysis.VisualBasic.CodeRefactoring.Testing",
        "Microsoft.CodeAnalysis.VisualBasic.CodeRefactoring.Testing.MSTest",
        "Microsoft.CodeAnalysis.VisualBasic.CodeRefactoring.Testing.NUnit",
        "Microsoft.CodeAnalysis.VisualBasic.CodeRefactoring.Testing.XUnit",
        "Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing",
        "Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing.MSTest",
        "Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing.NUnit",
        "Microsoft.CodeAnalysis.VisualBasic.SourceGenerators.Testing.XUnit"
    ];

    internal static async Task<int> PublishAsync(string repoName, string source, string apiKey, bool unlisted, bool skipDuplicate, ILogger logger)
    {
        try
        {
            var determinedVersion = repoName == RoslynRepo
                ? TryDetermineRoslynPackageVersion(out var version)
                : TryDetermineRoslynSdkPackageVersion(out version);

            if (!determinedVersion)
            {
                logger.LogError("Expected packages are missing. Unable to determine version.");
                return 1;
            }

            var packageIds = repoName == RoslynRepo
                ? RoslynPackageIds
                : RoslynSdkPackageIds;

            logger.LogInformation("Publishing {version} packages...", version);

            var skipDuplicateFlag = skipDuplicate ? "--skip-duplicate" : "";

            foreach (var packageId in packageIds)
            {
                var result = await PublishPackageAsync(packageId, version, skipDuplicateFlag);
                if (result.ExitCode != 0)
                {
                    logger.LogError("Failed to publish '{PackageId}'", packageId);
                    throw new InvalidOperationException(result.Output);
                }
                else
                {
                    logger.LogInformation("Package '{PackageId}' published.", packageId);
                }

                if (unlisted)
                {
                    await UnlistPackageAsync(packageId, version);
                    logger.LogInformation("Package '{PackageId}' unlisted.", packageId);
                }
            }

            logger.LogInformation("Packages published.");
        }
        catch (Exception ex)
        {
            logger.LogError("{Message}", ex.Message);
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

            version = Path.GetFileNameWithoutExtension(packageFileName)[32..];
            return true;
        }

        static bool TryDetermineRoslynSdkPackageVersion([NotNullWhen(returnValue: true)] out string? version)
        {
            var packageFileName = Directory.GetFiles(Environment.CurrentDirectory, "Microsoft.CodeAnalysis.Analyzer.Testing.*.nupkg").FirstOrDefault();
            if (packageFileName is null)
            {
                version = null;
                return false;
            }

            version = Path.GetFileNameWithoutExtension(packageFileName)[40..];
            return true;
        }

        Task<ProcessResult> PublishPackageAsync(string packageId, string? version, string skipDuplicatesFlag)
        {
            return ProcessRunner.RunProcessAsync("dotnet", $"nuget push {skipDuplicatesFlag} --source \"{source}\" --api-key \"{apiKey}\" \"{packageId}.{version}.nupkg\"");
        }

        Task<ProcessResult> UnlistPackageAsync(string packageId, string? version)
        {
            return ProcessRunner.RunProcessAsync("dotnet", $"nuget delete {packageId} {version} --source \"{source}\" --api-key \"{apiKey}\" --non-interactive");
        }
    }
}
