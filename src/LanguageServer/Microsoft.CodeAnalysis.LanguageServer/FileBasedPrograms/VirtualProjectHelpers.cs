// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;

internal static class VirtualProjectHelpers
{
    #region Temporary copy of subset of dotnet run-api behavior
    // See https://github.com/dotnet/sdk/blob/b5dbc69cc28676ac6ea615654c8016a11b75e747/src/Cli/Microsoft.DotNet.Cli.Utils/Sha256Hasher.cs#L10
    private static class Sha256Hasher
    {
        public static string Hash(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            byte[] hash = SHA256.HashData(bytes);
#if NET10_0_OR_GREATER
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

    internal static string GetDiscoveryCacheDirectory(string workspaceFolder)
        => GetTempPathCore("runfile-discovery", workspaceFolder);

    internal static string GetDiscoveryCacheRootDirectory()
        => GetTempDotnetSubdirectory("runfile-discovery");

    // See https://github.com/dotnet/sdk/blob/5a4292947487a9d34f4256c1d17fb3dc26859174/src/Cli/dotnet/Commands/Run/VirtualProjectBuildingCommand.cs#L449
    internal static string GetArtifactsPath(string entryPointFileFullPath)
        => GetTempPathCore("runfile", entryPointFileFullPath);

    private static string GetTempDotnetSubdirectory(string dotnetSubdirectory)
    {
        // We want a location where permissions are expected to be restricted to the current user.
        string tempDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.GetTempPath()
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Join(tempDirectory, "dotnet", dotnetSubdirectory);
    }

    private static string GetTempPathCore(string dotnetSubdirectory, string originalFilePath)
    {
        // Include original file name so the directory name is not completely opaque.
        string fileName = Path.GetFileNameWithoutExtension(originalFilePath);
        string hash = Sha256Hasher.HashWithNormalizedCasing(originalFilePath);
        string directoryName = $"{fileName}-{hash}";

        return Path.Join(GetTempDotnetSubdirectory(dotnetSubdirectory), directoryName);
    }

    #endregion
}
