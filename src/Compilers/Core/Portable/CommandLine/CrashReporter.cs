// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using Metalama.Compiler;

namespace Microsoft.CodeAnalysis.CommandLine
{
    internal static class CrashReporter
    {
        public static string? WriteCrashReport( Exception ex )
        {
            var crashReportDirectory = Path.Combine(MetalamaPathUtilities.GetTempPath(), "Metalama", "CrashReports");
            var crashReportPath = Path.Combine(crashReportDirectory, Guid.NewGuid() + ".txt");

 
            // Write the detailed file.
            try
            {
                var exceptionText = new StringBuilder();
                var process = Process.GetCurrentProcess();
               

                exceptionText.AppendLine($"Metalama.Compiler Version: {typeof(CrashReporter).Assembly.GetName().Version}");
                exceptionText.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
                exceptionText.AppendLine($"Processor Architecture: {RuntimeInformation.ProcessArchitecture}");
                exceptionText.AppendLine($"OS Description: {RuntimeInformation.OSDescription}");
                exceptionText.AppendLine($"OS Architecture: {RuntimeInformation.OSArchitecture}");
                exceptionText.AppendLine( $"Process Name: {process.ProcessName}" );
                exceptionText.AppendLine( $"Process Id: {process.Id}" );
                exceptionText.AppendLine( $"Command Line: {Environment.CommandLine}" );
                exceptionText.AppendLine($"Exception type: {ex.GetType()}");
                exceptionText.AppendLine($"Exception message: {ex.Message}");

                try
                {
                    // The next line may fail.
                    var exceptionToString = ex.ToString();
                    exceptionText.AppendLine("===== Exception ===== ");
                    exceptionText.AppendLine(exceptionToString);
                }
                catch { }

                exceptionText.AppendLine("===== Loaded assemblies ===== ");

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        exceptionText.AppendLine(assembly.Location);
                    }
                    catch { }
                }


                if (!Directory.Exists(crashReportDirectory))
                {
                    Directory.CreateDirectory(crashReportDirectory);
                }

                File.WriteAllText(crashReportPath, ex.ToString());

                return crashReportPath;
            }
            catch
            {
                return null;
            }
        }
    }
}
