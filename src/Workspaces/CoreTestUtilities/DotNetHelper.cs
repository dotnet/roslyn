// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Text;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public static class DotNetHelper
    {
        private static string RunProcess(string fileName, string arguments, string workingDirectory = null)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(fileName, arguments)
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory ?? string.Empty
                }
            };

            var output = new StringBuilder();

            process.OutputDataReceived += (_, e) => output.AppendLine(e.Data);
            process.ErrorDataReceived += (_, e) => output.AppendLine(e.Data);

            process.Start();

            process.WaitForExit();

            return output.ToString();
        }

        public static string Restore(string solutionOrProjectFileName, string workingDirectory = null)
        {
            return RunProcess("dotnet", $"restore {solutionOrProjectFileName}", workingDirectory);
        }
    }
}
