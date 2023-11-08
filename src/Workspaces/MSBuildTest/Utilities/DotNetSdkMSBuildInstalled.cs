// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Build.Locator;
using Roslyn.Test.Utilities;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;
#if NETCOREAPP
using System.Text.Json.Nodes;
#endif

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    internal partial class DotNetSdkMSBuildInstalled : ExecutionCondition
    {
        public static string? SdkPath { get; private set; } = null;

#if NETCOREAPP
        private static string? VersionString { get; } = null;

        static DotNetSdkMSBuildInstalled()
        {
            if (TryGetSDKVersion(out var version, out var rootPath))
            {
                VersionString = version;

                if (TryGetSdkPath(rootPath, version, out var sdkPath, out var msbuildPath))
                {
                    MSBuildLocator.RegisterMSBuildPath(msbuildPath);
                    SdkPath = sdkPath;
                }
            }
            else
            {
                // We don't have a global.json at all, but if we can find exactly one SDK version, we'll use that. This supports running in Helix.
                var instance = MSBuildLocator.QueryVisualStudioInstances(new VisualStudioInstanceQueryOptions { DiscoveryTypes = DiscoveryType.DotNetSdk }).SingleOrDefault();

                if (instance != null)
                {
                    MSBuildLocator.RegisterInstance(instance);
                }
            }

            return;

            static bool TryGetSDKVersion(
                [NotNullWhen(true)] out string? version,
                [NotNullWhen(true)] out DirectoryInfo? rootPath)
            {
                var parentDirectory = new DirectoryInfo(Path.GetDirectoryName(typeof(DotNetSdkMSBuildInstalled).Assembly.Location)!);
                while (parentDirectory != null)
                {
                    var globalJsonPath = Path.Combine(parentDirectory.FullName, "global.json");
                    if (File.Exists(globalJsonPath))
                    {
                        var globalJsonString = File.ReadAllText(globalJsonPath);
                        version = JsonNode.Parse(globalJsonString)
                            ?["sdk"]
                            ?["version"]
                            ?.GetValue<string>();
                        rootPath = parentDirectory;
                        return version is not null;
                    }

                    parentDirectory = parentDirectory.Parent;
                }

                version = null;
                rootPath = null;
                return false;
            }

            static bool TryGetSdkPath(
                DirectoryInfo rootDirectory,
                string version,
                [NotNullWhen(true)] out string? sdkPath,
                [NotNullWhen(true)] out string? msbuildPath)
            {
                sdkPath = null;
                msbuildPath = null;

                // use the local SDK if its there
                var sdkFolder = Path.Combine(rootDirectory.FullName, ".dotnet");
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
        }
#endif

        public DotNetSdkMSBuildInstalled()
        {
        }

        // Eventually should revert condition to 'SdkPath is null' after MSBuild issue is fixed:
        // https://github.com/dotnet/roslyn/issues/67566
        public override bool ShouldSkip
            => true;

        public override string SkipReason
#if NETCOREAPP
            => $"Could not locate .NET SDK version {VersionString}.";
#else
            => $"Test runs on .NET Core only.";
#endif
    }
}
