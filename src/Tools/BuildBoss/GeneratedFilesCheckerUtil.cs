// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;

namespace BuildBoss;

internal sealed class GeneratedFilesCheckerUtil : ICheckerUtil
{
    private readonly string _targetDir;

    internal GeneratedFilesCheckerUtil(string targetDir)
    {
        _targetDir = targetDir;
    }

    public bool Check(TextWriter textWriter)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "status --porcelain",
                WorkingDirectory = _targetDir,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            using var process = Process.Start(startInfo);
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            var output = outputTask.Result;
            var error = errorTask.Result;

            if (process.ExitCode != 0)
            {
                textWriter.WriteLine($"git status failed with exit code {process.ExitCode}");
                WriteOutput(textWriter, output, error);
                return false;
            }

            if (output.Length > 0)
            {
                textWriter.WriteLine("Generated files are out of date. Build the solution and commit the generated changes.");
                WriteOutput(textWriter, output, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            textWriter.WriteLine($"Error running git status: {ex}");
            return false;
        }
    }

    private static void WriteOutput(TextWriter textWriter, string output, string error)
    {
        if (output.Length > 0)
        {
            textWriter.Write(output);
        }

        if (error.Length > 0)
        {
            textWriter.Write(error);
        }
    }
}
