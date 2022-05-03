// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.RoslynTools.Authentication.PopUps
{
    internal static class LocalHelpers
    {
        public static string GetEditorPath(string gitLocation, ILogger logger)
        {
            string editor = ExecuteCommand(gitLocation, "config --get core.editor", logger);

            // If there is nothing set in core.editor we try to default it to code
            if (string.IsNullOrEmpty(editor))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    editor = ExecuteCommand("where", "code", logger);
                }
                else
                {
                    editor = ExecuteCommand("which", "code", logger);
                }
            }

            // If there is nothing set in core.editor we try to default it to notepad if running in Windows, if not default it to
            // vim
            if (string.IsNullOrEmpty(editor))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    editor = ExecuteCommand("where", "notepad", logger);
                }
                else
                {
                    editor = ExecuteCommand("which", "vim", logger);
                }
            }

            // Split this by newline in case where are multiple paths;
            int newlineIndex = editor.IndexOf(System.Environment.NewLine);
            if (newlineIndex != -1)
            {
                editor = editor[..newlineIndex];
            }

            return editor;
        }

        public static string GetRootDir(string gitLocation, ILogger logger)
        {
            string dir = ExecuteCommand(gitLocation, "rev-parse --show-toplevel", logger);

            if (string.IsNullOrEmpty(dir))
            {
                throw new Exception("Root directory of the repo was not found. Check that git is installed and that you are in a folder which is a git repo (.git folder should be present).");
            }

            return dir;
        }

        /// <summary>
        ///     Get the current git commit sha.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static string GetGitCommit(string gitLocation, ILogger logger)
        {
            string commit = ExecuteCommand(gitLocation, "rev-parse HEAD", logger);

            if (string.IsNullOrEmpty(commit))
            {
                throw new Exception("Commit was not resolved. Check if git is installed and that a .git directory exists in the root of your repository.");
            }

            return commit;
        }

        public static string GitShow(string gitLocation, string repoFolderPath, string commit, string fileName, ILogger logger)
        {
            string fileContents = ExecuteCommand(gitLocation, $"show {commit}:{fileName}", logger, repoFolderPath);

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
        /// <returns></returns>
        public static string GetRepoPathFromFolder(string gitLocation, string sourceFolder, string commit, ILogger logger)
        {
            foreach (string directory in Directory.GetDirectories(sourceFolder))
            {
                string containsCommand = ExecuteCommand(gitLocation, $"branch --contains {commit}", logger, directory);

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
            string versionInfo = ExecuteCommand(gitLocation, "version --build-options", logger);

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

            string output = string.Empty;

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
}
