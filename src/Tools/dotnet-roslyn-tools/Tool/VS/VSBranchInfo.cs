// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Authentication;
using Microsoft.RoslynTools.Products;
using Microsoft.RoslynTools.Utilities;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.RoslynTools.VS;

internal static class VSBranchInfo
{
    public static IProduct[] AllProducts = new IProduct[]
    {
        new Roslyn(),
        new Razor(),
        new TypeScript(),
        new FSharp(),
    };

    public static async Task<int> GetInfoAsync(string gitVersion, GitVersionType gitVersionType, string product, bool showArtifacts, RoslynToolsSettings settings, ILogger logger)
    {
        try
        {
            using var devdivConnection = new AzDOConnection(settings.DevDivAzureDevOpsBaseUri, "DevDiv", settings.DevDivAzureDevOpsToken);
            using var dncengConnection = new AzDOConnection(settings.DncEngAzureDevOpsBaseUri, "internal", settings.DncEngAzureDevOpsToken);

            foreach (var productConfig in AllProducts)
            {
                if (product.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                    product.Equals(productConfig.Name, StringComparison.OrdinalIgnoreCase))
                {
                    WriteHeader($"{productConfig.Name} info from VS {gitVersionType} {gitVersion}");

                    await WritePackageInfo(productConfig, gitVersion, gitVersionType, devdivConnection);
                    await WriteBuildInfo(productConfig, gitVersion, gitVersionType, showArtifacts, devdivConnection, dncengConnection);

                    Console.WriteLine();
                }
            }
        }
        catch (VssUnauthorizedException vssEx)
        {
            logger.LogError(vssEx, "Authentication error occurred: {Message}. Run `roslyn-tools authenticate` to configure the AzDO authentication tokens.", vssEx.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred: {Message}", ex.Message);
        }

        return 1;
    }

    private static async Task WritePackageInfo(IProduct product, string gitVersion, GitVersionType gitVersionType, AzDOConnection devdiv)
    {
        if (product.PackageName is null)
        {
            return;
        }

        var packageVersion = await VisualStudioRepository.GetPackageVersionFromDefaultConfigAsync(gitVersion, gitVersionType, devdiv, product.PackageName);

        WriteNameAndValue("Package Version", packageVersion);
    }

    private static async Task WriteBuildInfo(IProduct product, string gitVersion, GitVersionType gitVersionType, bool showArtifacts, AzDOConnection devdiv, AzDOConnection dnceng)
    {
        var buildNumber = await VisualStudioRepository.GetBuildNumberFromComponentJsonFileAsync(gitVersion, gitVersionType, devdiv, product.ComponentJsonFileName, product.ComponentName);

        // Try getting build info from dnceng first
        var buildConnection = dnceng;
        var builds = await TryGetBuildsAsync(product, buildConnection, buildNumber);
        if (builds is null)
        {
            // if that fails, try devdiv
            buildConnection = devdiv;
            builds = await TryGetBuildsAsync(product, buildConnection, buildNumber);
        }

        if (builds is null or { Count: 0 })
        {
            throw new Exception($"Couldn't find build for build number: {buildNumber}");
        }

        foreach (var build in builds)
        {
            WriteNameAndValue("Build Number", build.BuildNumber);
            WriteNameAndValue("Commit SHA", build.SourceVersion);
            WriteNameAndValue("Link", $"{product.RepoHttpBaseUrl}/commit/{build.SourceVersion}", "    ");
            WriteNameAndValue("Source Branch", build.SourceBranch.Replace("refs/heads/", ""));
            WriteNameAndValue("Build", ((ReferenceLink)build.Links.Links["web"]).Href);

            if (showArtifacts)
            {
                await WriteArtifactInfo(product, buildConnection, build);
            }
        }
    }

    private static async Task<List<Build>?> TryGetBuildsAsync(IProduct product, AzDOConnection buildConnection, string buildNumber)
    {
        var pipelineName = product.GetBuildPipelineName(buildConnection.BuildProjectName);
        if (pipelineName is not null)
        {
            var builds = await buildConnection.TryGetBuildsAsync(pipelineName, buildNumber);

            if (builds is not null)
            {
                return builds;
            }
        }

        return null;
    }

    // Inspired by Mitch Denny: https://dev.azure.com/mseng/AzureDevOps/_git/ArtifactTool?path=/src/ArtifactTool/Commands/PipelineArtifacts/PipelineArtifactDownloadCommand.cs&version=GBusers/midenn/fcs-integration&line=68&lineEnd=69&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
    // Note: He wishes not to have his name attached to it.
    private static async Task WriteArtifactInfo(IProduct product, AzDOConnection connection, Build build)
    {
        if (product.ArtifactsFolderName is null || product.ArtifactsSubFolderNames.Length == 0)
            return;

        var artifact = await connection.BuildClient.GetArtifactAsync(build.Project.Id, build.Id, product.ArtifactsFolderName);

        var (id, name) = ParseContainerId(artifact.Resource.Data);

        var projectId = (await connection.ProjectClient.GetProject(connection.BuildProjectName)).Id;

        var items = await connection.ContainerClient.QueryContainerItemsAsync(id, projectId, name);

        var links = from i in items
                    where i.ItemType == VisualStudio.Services.FileContainer.ContainerItemType.Folder
                    where product.ArtifactsSubFolderNames.Contains(i.Path)
                    select (i.Path.Split('/').Last(), i.ContentLocation + "&%24format=zip&saveAbsolutePath=false"); // %24 == $

        WriteNamesAndValues("Packages", links);

        static (long id, string name) ParseContainerId(string resourceData)
        {
            // Example of resourceData: "#/7029766/artifacttool-alpine-x64-Debug"
            var segments = resourceData.Split('/');

            if (segments.Length == 3 && segments[0] == "#" && long.TryParse(segments[1], out var containerId))
            {
                return (containerId, segments[2]);
            }

            throw new Exception($"Resource data value '{resourceData}' was not in expected format.");
        }
    }

    private static void WriteHeader(string branch)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{branch}:");
        Console.ResetColor();
    }

    private static void WriteNameAndValue(string name, string value, string indent = "  ")
    {
        Console.Write($"{indent}{name}: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(value);
        Console.ResetColor();
    }

    private static void WriteNamesAndValues(string header, IEnumerable<(string, string)> values)
    {
        Console.WriteLine($"  {header}:");
        foreach (var (name, value) in values)
        {
            WriteNameAndValue(name, value, "    ");
        }
    }
}
