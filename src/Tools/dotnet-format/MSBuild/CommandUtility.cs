// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// LICENSING NOTE: The license for this file is from the originating 
// source and not the general https://github.com/dotnet/roslyn license.
// See https://github.com/dotnet/docfx/blob/3ffbfe6ecca20850cacf6191586f14819f9f4b03/src/Microsoft.DocAsCode.Common/CommandUtility.cs

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Tools.MSBuild
{
    internal static class CommandUtility
    {
        public static int RunCommand(CommandInfo commandInfo, StreamWriter stdoutWriter = null, StreamWriter stderrWriter = null, int timeoutInMilliseconds = Timeout.Infinite)
        {
            if (commandInfo == null)
            {
                throw new ArgumentNullException(nameof(commandInfo));
            }

            if (timeoutInMilliseconds < 0 && timeoutInMilliseconds != Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutInMilliseconds), $"{nameof(timeoutInMilliseconds)} must be greater than or equal to zero, or equal to {Timeout.Infinite}.");
            }

            using (var process = new Process())
            {
                process.StartInfo.FileName = commandInfo.Name;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.Arguments = commandInfo.Arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.WorkingDirectory = commandInfo.WorkingDirectory;

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.Start();

                Task outputTask = null;
                if (stdoutWriter != null)
                {
                    var errorMessage = $"Unable to write standard output when running command {commandInfo.Name}";
                    outputTask = Task.Run(() => PipeOutput(process.StandardOutput, stdoutWriter, errorMessage));
                }

                Task errorTask = null;
                if (stderrWriter != null)
                {
                    var errorMessage = $"Unable to write standard error output when running command {commandInfo.Name}";
                    errorTask = Task.Run(() => PipeOutput(process.StandardError, stderrWriter, errorMessage));
                }

                try
                {
                    if (process.WaitForExit(timeoutInMilliseconds))
                    {
                        return process.ExitCode;
                    }
                    else
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                }
                finally
                {
                    outputTask?.Wait();
                    errorTask?.Wait();
                }
            }
            return 0;
        }

        internal static void PipeOutput(StreamReader reader, StreamWriter writer, string errorMessage)
        {
            const int bufferSize = 512;

            var buffer = new char[bufferSize];
            while (true)
            {
                var index = reader.Read(buffer, 0, bufferSize);
                if (index > 0)
                {
                    try
                    {
                        writer.Write(buffer, 0, index);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"{errorMessage}: {ex.Message}");
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public static bool ExistCommand(string commandName)
        {
            const int timeout = 1000;

            int exitCode;
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                exitCode = RunCommand(new CommandInfo
                {
                    Name = "type",
                    Arguments = commandName
                }, timeoutInMilliseconds: timeout);
            }
            else
            {
                exitCode = RunCommand(new CommandInfo
                {
                    Name = "where",
                    Arguments = commandName
                }, timeoutInMilliseconds: timeout);
            }
            return exitCode == 0;
        }
    }

    public class CommandInfo
    {
        public string Name { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
    }
}
