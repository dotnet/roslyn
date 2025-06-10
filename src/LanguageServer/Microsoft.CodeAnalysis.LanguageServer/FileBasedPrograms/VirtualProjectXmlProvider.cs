﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

[Export(typeof(VirtualProjectXmlProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class VirtualProjectXmlProvider(DotnetCliHelper dotnetCliHelper, ILoggerFactory loggerFactory)
{
    private readonly ILogger<VirtualProjectXmlProvider> _logger = loggerFactory.CreateLogger<VirtualProjectXmlProvider>();

    internal async Task<(string VirtualProjectXml, ImmutableArray<SimpleDiagnostic> Diagnostics)?> GetVirtualProjectContentAsync(string documentFilePath, CancellationToken cancellationToken)
    {
        var workingDirectory = Path.GetDirectoryName(documentFilePath);
        var process = dotnetCliHelper.Run(["run-api"], workingDirectory, shouldLocalizeOutput: true, redirectStandardInput: true);

        cancellationToken.Register(() =>
        {
            process?.Kill();
        });

        var input = new RunApiInput.GetProject() { EntryPointFileFullPath = documentFilePath };
        var inputJson = JsonSerializer.Serialize(input, RunFileApiJsonSerializerContext.Default.RunApiInput);
        await process.StandardInput.WriteAsync(inputJson);
        process.StandardInput.Close();

        // Debug severity is used for these because we think it will be common for the user environment to have too old of an SDK for the call to work.
        // Rather than representing a hard error condition, it represents a condition where we need to gracefully downgrade the experience.
        process.ErrorDataReceived += (sender, args) => _logger.LogDebug($"dotnet run-api: {args.Data}");
        process.BeginErrorReadLine();

        var responseJson = await process.StandardOutput.ReadLineAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            _logger.LogDebug($"dotnet run-api exited with exit code '{process.ExitCode}'.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(responseJson))
        {
            _logger.LogError($"dotnet run-api exited with exit code 0, but did not return any response.");
            return null;
        }

        try
        {
            var response = JsonSerializer.Deserialize(responseJson, RunFileApiJsonSerializerContext.Default.RunApiOutput);
            if (response is RunApiOutput.Error error)
            {
                _logger.LogError($"dotnet run-api version: {error.Version}. Latest known version: {RunApiOutput.LatestKnownVersion}");
                _logger.LogError($"dotnet run-api returned error: '{error.Message}'");
                return null;
            }

            if (response is RunApiOutput.Project project)
            {
                if (project.Version > RunApiOutput.LatestKnownVersion)
                {
                    _logger.LogWarning($"'dotnet run-api' version '{project.Version}' is newer than latest known version {RunApiOutput.LatestKnownVersion}");
                }

                return (project.Content, project.Diagnostics);
            }

            throw ExceptionUtilities.UnexpectedValue(response);
        }
        catch (JsonException ex)
        {
            // In this case, run-api returned 0 exit code, but gave us back JSON that we don't know how to parse.
            _logger.LogError(ex, "Could not deserialize run-api response.");
            return null;
        }
    }

    /// <summary>
    /// Adjusts a path to a file-based program for use in passing the virtual project to msbuild.
    /// (msbuild needs the path to end in .csproj to recognize as a C# project and apply all the standard props/targets to it.)
    /// </summary>
    internal static string GetVirtualProjectPath(string documentFilePath)
        => Path.ChangeExtension(documentFilePath, ".csproj");

    internal static bool IsFileBasedProgram(string documentFilePath, SourceText text)
    {
        // TODO: this needs to be adjusted to be more sustainable.
        // When we adopt the dotnet run-api, we need to get rid of this or adjust it to be more sustainable (e.g. using the appropriate document to get a syntax tree)
        var tree = CSharpSyntaxTree.ParseText(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), path: documentFilePath);
        var root = tree.GetRoot();
        var isFileBasedProgram = root.GetLeadingTrivia().Any(SyntaxKind.IgnoredDirectiveTrivia) || root.ChildNodes().Any(node => node.IsKind(SyntaxKind.GlobalStatement));
        return isFileBasedProgram;
    }

    #region Temporary copy of subset of dotnet run-api behavior for fallback: https://github.com/dotnet/roslyn/issues/78618
    // See https://github.com/dotnet/sdk/blob/b5dbc69cc28676ac6ea615654c8016a11b75e747/src/Cli/Microsoft.DotNet.Cli.Utils/Sha256Hasher.cs#L10
    private static class Sha256Hasher
    {
        public static string Hash(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            byte[] hash = SHA256.HashData(bytes);
#if NET9_0_OR_GREATER
            return Convert.ToHexStringLower(hash);
#else
            return Convert.ToHexString(hash).ToLowerInvariant();
#endif
        }

        public static string HashWithNormalizedCasing(string text)
        {
            return Hash(text.ToUpperInvariant());
        }
    }

    // See https://github.com/dotnet/sdk/blob/5a4292947487a9d34f4256c1d17fb3dc26859174/src/Cli/dotnet/Commands/Run/VirtualProjectBuildingCommand.cs#L449
    internal static string GetArtifactsPath(string entryPointFileFullPath)
    {
        // We want a location where permissions are expected to be restricted to the current user.
        string directory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.GetTempPath()
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Include entry point file name so the directory name is not completely opaque.
        string fileName = Path.GetFileNameWithoutExtension(entryPointFileFullPath);
        string hash = Sha256Hasher.HashWithNormalizedCasing(entryPointFileFullPath);
        string directoryName = $"{fileName}-{hash}";

        return Path.Join(directory, "dotnet", "runfile", directoryName);
    }
    #endregion

    // https://github.com/dotnet/roslyn/issues/78618: falling back to this until dotnet run-api is more widely available
    internal static string MakeVirtualProjectContent_DirectFallback(string documentFilePath)
    {
        Contract.ThrowIfFalse(PathUtilities.IsAbsolute(documentFilePath));
        var artifactsPath = GetArtifactsPath(documentFilePath);

        var targetFramework = Environment.GetEnvironmentVariable("DOTNET_RUN_FILE_TFM") ?? "net10.0";

        var virtualProjectXml = $"""
            <Project>
              <PropertyGroup>
                <IncludeProjectNameInArtifactsPaths>false</IncludeProjectNameInArtifactsPaths>
                <ArtifactsPath>{SecurityElement.Escape(artifactsPath)}</ArtifactsPath>
              </PropertyGroup>
              <!-- We need to explicitly import Sdk props/targets so we can override the targets below. -->
              <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>{SecurityElement.Escape(targetFramework)}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <PropertyGroup>
                <EnableDefaultItems>false</EnableDefaultItems>
              </PropertyGroup>
              <PropertyGroup>
                <LangVersion>preview</LangVersion>
              </PropertyGroup>
              <PropertyGroup>
                <Features>$(Features);FileBasedProgram</Features>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="{SecurityElement.Escape(documentFilePath)}" />
              </ItemGroup>
              <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
              <!--
                Override targets which don't work with project files that are not present on disk.
                See https://github.com/NuGet/Home/issues/14148.
              -->
              <Target Name="_FilterRestoreGraphProjectInputItems"
                      DependsOnTargets="_LoadRestoreGraphEntryPoints"
                      Returns="@(FilteredRestoreGraphProjectInputItems)">
                <ItemGroup>
                  <FilteredRestoreGraphProjectInputItems Include="@(RestoreGraphProjectInputItems)" />
                </ItemGroup>
              </Target>
              <Target Name="_GetAllRestoreProjectPathItems"
                      DependsOnTargets="_FilterRestoreGraphProjectInputItems"
                      Returns="@(_RestoreProjectPathItems)">
                <ItemGroup>
                  <_RestoreProjectPathItems Include="@(FilteredRestoreGraphProjectInputItems)" />
                </ItemGroup>
              </Target>
              <Target Name="_GenerateRestoreGraph"
                      DependsOnTargets="_FilterRestoreGraphProjectInputItems;_GetAllRestoreProjectPathItems;_GenerateRestoreGraphProjectEntry;_GenerateProjectRestoreGraph"
                      Returns="@(_RestoreGraphEntry)">
                <!-- Output from dependency _GenerateRestoreGraphProjectEntry and _GenerateProjectRestoreGraph -->
              </Target>
            </Project>
            """;

        return virtualProjectXml;
    }
}
