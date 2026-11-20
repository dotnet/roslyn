// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.RoslynTools.Authentication.PopUps;

internal static class LocalHelpers
{
    public static string GetRootDir(string gitLocation, ILogger logger)
    {
        var dir = ExecuteCommand(gitLocation, "rev-parse --show-toplevel", logger);

        if (string.IsNullOrEmpty(dir))
        {
            throw new Exception("Root directory of the repo was not found. Check that git is installed and that you are in a folder which is a git repo (.git folder should be present).");
        }

        return dir;
    }

    /// <summary>
    /// Get the current git commit sha.
    /// </summary>
    public static string GetGitCommit(string gitLocation, ILogger logger)
    {
        var commit = ExecuteCommand(gitLocation, "rev-parse HEAD", logger);

        if (string.IsNullOrEmpty(commit))
        {
            throw new Exception("Commit was not resolved. Check if git is installed and that a .git directory exists in the root of your repository.");
        }

        return commit;
    }

    public static string GitShow(string gitLocation, string repoFolderPath, string commit, string fileName, ILogger logger)
    {
        var fileContents = ExecuteCommand(gitLocation, $"show {commit}:{fileName}", logger, repoFolderPath);

        if (string.IsNullOrEmpty(fileContents))
        {
            throw new Exception($"Could not show the contents of '{fileName}' at '{commit}' in '{repoFolderPath}'...");
        }

        return fileContents;
    }

    /// <summary>
    /// For each child folder in the provided "source" folder we check for the existance of a given commit. Each folder in "source"
    /// represent a different repo.
    /// </summary>
    /// <param name="sourceFolder">The main source folder.</param>
    /// <param name="commit">The commit to search for in a repo folder.</param>
    /// <param name="logger">The logger.</param>
    public static string GetRepoPathFromFolder(string gitLocation, string sourceFolder, string commit, ILogger logger)
    {
        foreach (var directory in Directory.GetDirectories(sourceFolder))
        {
            var containsCommand = ExecuteCommand(gitLocation, $"branch --contains {commit}", logger, directory);

            if (!string.IsNullOrEmpty(containsCommand))
            {
                return directory;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Check that the git installation is valid by running git version --build-options
    /// and checking the outputs to confirm that it is well-formed
    /// </summary>
    /// <param name="gitLocation">The location of git.exe</param>
    /// <param name="logger">The logger</param>
    public static void CheckGitInstallation(string gitLocation, ILogger logger)
    {
        var versionInfo = ExecuteCommand(gitLocation, "version --build-options", logger);

        if (!versionInfo.StartsWith("git version") || !versionInfo.Contains("cpu:"))
        {
            throw new Exception($"Something failed when validating the git installation {gitLocation}");
        }
    }

    public static string ExecuteCommand(string command, string arguments, ILogger logger, string? workingDirectory = null)
    {
        if (string.IsNullOrEmpty(command))
        {
            throw new ArgumentException("Executable command must be non-empty");
        }

        var output = string.Empty;

        try
        {
            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = command,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
            };

            using var process = new Process();
            process.StartInfo = processInfo;
            process.StartInfo.Arguments = arguments;
            process.Start();

            output = process.StandardOutput.ReadToEnd().Trim();

            process.WaitForExit();
        }
        catch (Exception exc)
        {
            logger.LogWarning("Something failed while trying to execute '{Command} {Arguments}'. Exception: {Message}", command, arguments, exc.Message);
        }

        return output;
    }
}
