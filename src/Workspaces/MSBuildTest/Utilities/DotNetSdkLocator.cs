// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests;

internal class DotNetSdkLocator
{
    public static string? SdkPath { get; private set; } = null;

    static DotNetSdkLocator()
    {
        var solutionFolder = GetSolutionFolder();

        if (!TryGetSDKVersion(solutionFolder, out var version))
        {
            return;
        }

        if (TryGetSdkPath(solutionFolder, version, out var sdkPath, out var msbuildPath))
        {
            SdkPath = sdkPath;
        }

        return;

        static bool TryGetSDKVersion(
            string solutionFolder,
            [NotNullWhen(true)] out string? version)
        {
            var globalJsonPath = Path.Combine(solutionFolder, "global.json");
            var globalJsonString = File.ReadAllText(globalJsonPath);
            version = JsonNode.Parse(globalJsonString)
                ?["sdk"]
                ?["version"]
                ?.GetValue<string>();
            return version is not null;
        }

        static bool TryGetSdkPath(
            string solutionFolder,
            string version,
            [NotNullWhen(true)] out string? sdkPath,
            [NotNullWhen(true)] out string? msbuildPath)
        {
            sdkPath = null;
            msbuildPath = null;

            // use the local SDK if its there
            var sdkFolder = Path.Combine(solutionFolder, ".dotnet");
            var localSDK = Path.Combine(sdkFolder, "sdk", version);
            if (Directory.Exists(localSDK))
            {
                sdkPath = sdkFolder;
                msbuildPath = localSDK;
                return true;
            }

            // check if sdk is installed
            var programFilesSDKFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");
            var programFilesSDK = Path.Combine(
                programFilesSDKFolder, "sdk", version);
            if (Directory.Exists(programFilesSDK))
            {
                sdkPath = programFilesSDKFolder;
                msbuildPath = programFilesSDK;
                return true;
            }

            var specifiedSDKPath = Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR");
            if (string.IsNullOrEmpty(specifiedSDKPath))
            {
                return false;
            }

            var specifiedSDKFolder = Path.Combine(
            specifiedSDKPath,
            "dotnet", "sdk", version);
            if (Directory.Exists(specifiedSDKFolder))
            {
                sdkPath = specifiedSDKPath;
                msbuildPath = specifiedSDKFolder;
                return true;
            }

            return false;
        }

        static string GetSolutionFolder()
        {
            // Expected assembly path:
            //  <solutionFolder>\artifacts\bin\Microsoft.CodeAnalysis.Workspaces.MSBuild.UnitTests\<Configuration>\<TFM>\Microsoft.CodeAnalysis.Workspaces.MSBuild.UnitTests.dll
            var assemblyLocation = typeof(DotNetSdkMSBuildInstalled).Assembly.Location;
            var solutionFolder = Directory.GetParent(assemblyLocation)
                ?.Parent?.Parent?.Parent?.Parent?.Parent?.FullName;
            Assumes.NotNull(solutionFolder);
            return solutionFolder;
        }
    }
}
