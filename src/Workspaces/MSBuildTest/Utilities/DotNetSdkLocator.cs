// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Nodes;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests;

internal class DotNetSdkLocator
{
    public static string? SdkVersion { get; private set; } = null;
    public static string? SdkPath { get; private set; } = null;

    static DotNetSdkLocator()
    {
        if (TryGetGlobalJsonPath(out var globalJsonPath)
            && TryGetSDKVersion(globalJsonPath, out var version)
            && TryGetSdkPath(globalJsonPath, version, out var sdkPath))
        {
            SdkVersion = version;
            SdkPath = sdkPath;
        }

        return;

        static bool TryGetGlobalJsonPath(
            [NotNullWhen(true)] out string? globalJsonPath)
        {
            globalJsonPath = null;

            var path = typeof(DotNetSdkMSBuildInstalled).Assembly.Location;
            while (path is not null)
            {
                var possibleGlobalJsonPath = Path.Combine(path, "global.json");
                if (File.Exists(possibleGlobalJsonPath))
                {
                    globalJsonPath = possibleGlobalJsonPath;
                    return true;
                }
                path = Path.GetDirectoryName(path);
            }

            return false;
        }

        static bool TryGetSDKVersion(
            string globalJsonPath,
            [NotNullWhen(true)] out string? version)
        {
            var globalJsonString = File.ReadAllText(globalJsonPath);
            version = JsonNode.Parse(globalJsonString)
                ?["sdk"]
                ?["version"]
                ?.GetValue<string>();
            return version is not null;
        }

        static bool TryGetSdkPath(
            string globalJsonPath,
            string version,
            [NotNullWhen(true)] out string? sdkPath)
        {
            sdkPath = null;

            // use the local SDK if its there
            var rootFolder = Path.GetDirectoryName(globalJsonPath)!;
            var sdkFolder = Path.Combine(rootFolder, ".dotnet");
            var localSDK = Path.Combine(sdkFolder, "sdk", version);
            if (Directory.Exists(localSDK))
            {
                sdkPath = sdkFolder;
                return true;
            }

            // check if sdk is installed
            var programFilesSDKFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");
            var programFilesSDK = Path.Combine(programFilesSDKFolder, "sdk", version);
            if (Directory.Exists(programFilesSDK))
            {
                sdkPath = programFilesSDKFolder;
                return true;
            }

            var specifiedSDKPath = Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR");
            if (string.IsNullOrEmpty(specifiedSDKPath))
            {
                return false;
            }

            var specifiedSDKFolder = Path.Combine(specifiedSDKPath, "dotnet", "sdk", version);
            if (Directory.Exists(specifiedSDKFolder))
            {
                sdkPath = specifiedSDKPath;
                return true;
            }

            return false;
        }
    }
}
