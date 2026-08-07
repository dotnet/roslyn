// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BuildBoss
{
    internal static class SharedUtil
    {
        internal static string MSBuildNamespaceUriRaw => "http://schemas.microsoft.com/developer/msbuild/2003";
        internal static Uri MSBuildNamespaceUri { get; } = new Uri(MSBuildNamespaceUriRaw);
        internal static XNamespace MSBuildNamespace { get; } = XNamespace.Get(MSBuildNamespaceUriRaw);
        internal static Encoding Encoding { get; } = Encoding.UTF8;

        internal static bool IsSolutionFile(string path) => Path.GetExtension(path) == ".sln";
        internal static bool IsPropsFile(string path) => Path.GetExtension(path) == ".props";
        internal static bool IsTargetsFile(string path) => Path.GetExtension(path) == ".targets";
        internal static bool IsXslt(string path) => Path.GetExtension(path) == ".xslt";

        /// <summary>
        /// Finds the single NuPkg in <paramref name="directory"/> whose file name begins with
        /// <paramref name="partialName"/> followed by a version.
        /// </summary>
        internal static string FindNuGetPackage(string directory, string partialName)
        {
            var regex = $@"^{Regex.Escape(partialName)}\.\d.*\.nupkg$";
            var files = Directory
                .EnumerateFiles(directory, "*.nupkg")
                .Where(filePath => Regex.IsMatch(Path.GetFileName(filePath), regex))
                .ToArray();

            if (files.Length == 0)
            {
                throw new Exception($"Unable to find '{partialName}' in '{directory}'");
            }

            if (files.Length > 1)
            {
                throw new Exception($"Found multiple packages for '{partialName}' in '{directory}':{Environment.NewLine}{string.Join(Environment.NewLine, files)}");
            }

            return files[0];
        }

        /// <summary>
        /// Extracts the version from a NuPkg file path produced by our build. The file name has the
        /// form "{packageId}.{version}.nupkg" and the version always begins with a digit.
        /// </summary>
        internal static string GetNuGetPackageVersion(string packageFilePath, string packageId)
        {
            var fileName = Path.GetFileNameWithoutExtension(packageFilePath);
            var prefix = packageId + ".";
            if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"NuPkg file name '{fileName}' does not begin with package id '{packageId}'");
            }

            return fileName.Substring(prefix.Length);
        }
    }

    internal readonly struct ProcessResult
    {
        internal int ExitCode { get; }
        internal string Output { get; }

        internal bool Succeeded => ExitCode == 0;

        internal ProcessResult(int exitCode, string output)
        {
            ExitCode = exitCode;
            Output = output;
        }
    }

    internal static class ProcessUtil
    {
        /// <summary>
        /// Runs <paramref name="fileName"/> to completion, capturing stdout and stderr into a single
        /// stream. Output is captured rather than inherited so that callers can include it in a
        /// failure report without polluting the log on success.
        /// </summary>
        internal static ProcessResult Run(string fileName, string arguments, string workingDirectory)
        {
            var startInfo = new ProcessStartInfo(fileName, arguments)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            var output = new StringBuilder();
            using var process = new Process { StartInfo = startInfo };

            // Read both streams asynchronously; reading them synchronously in sequence can deadlock
            // when a child fills the pipe buffer of the stream we aren't currently draining.
            process.OutputDataReceived += appendLine;
            process.ErrorDataReceived += appendLine;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            return new ProcessResult(process.ExitCode, output.ToString());

            void appendLine(object sender, DataReceivedEventArgs e)
            {
                if (e.Data is not null)
                {
                    lock (output)
                    {
                        output.AppendLine(e.Data);
                    }
                }
            }
        }
    }
}
