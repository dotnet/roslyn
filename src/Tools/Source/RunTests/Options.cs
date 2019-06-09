// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RunTests
{
    internal enum Display
    {
        None,
        All,
        Succeeded,
        Failed,
    }

    internal class Options
    {
        /// <summary>
        /// Use HTML output files.
        /// </summary>
        public bool UseHtml { get; set; }

        /// <summary>
        /// Use the 64 bit test runner.
        /// </summary>
        public bool Test64 { get; set; }

        /// <summary>
        /// Target framework used to run the tests, e.g. "net472".
        /// This is currently only used to name the test result files.
        /// </summary>
        public string TargetFrameworkMoniker { get; set; }

        /// <summary>
        /// Use the open integration test runner.
        /// </summary>
        public bool TestVsi { get; set; }

        /// <summary>
        /// Allow the caching of test results.
        /// </summary>
        public bool UseCachedResults { get; set; }

        /// <summary>
        /// Display the results files.
        /// </summary>
        public Display Display { get; set; }

        /// <summary>
        /// Trait string to pass to xunit.
        /// </summary>
        public string Trait { get; set; }

        /// <summary>
        /// The no-trait string to pass to xunit.
        /// </summary>
        public string NoTrait { get; set; }

        /// <summary>
        /// Set of assemblies to test.
        /// </summary>
        public List<string> Assemblies { get; set; }

        /// <summary>
        /// Time after which the runner should kill the xunit process and exit with a failure.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Whether or not to use proc dump to monitor running processes for failures.
        /// </summary>
        public bool UseProcDump { get; set; }

        /// <summary>
        /// The directory which contains procdump.exe. 
        /// </summary>
        public string ProcDumpDirectory { get; set; }

        public string XunitPath { get; set; }

        /// <summary>
        /// Directory to hold all of the xml files created as test results.
        /// </summary>
        public string TestResultXmlOutputDirectory { get; set; }

        /// <summary>
        /// Directory to hold dump files and other log files created while running tests.
        /// </summary>
        public string LogFilesOutputDirectory { get; set; }

        /// <summary>
        /// Directory to hold secondary dump files created while running tests.
        /// </summary>
        public string LogFilesSecondaryOutputDirectory { get; set; }

        internal static Options Parse(string[] args)
        {
            if (args == null || args.Any(a => a == null) || args.Length < 2)
            {
                return null;
            }

            var comparer = StringComparer.OrdinalIgnoreCase;
            bool isOption(string argument, string optionName, out string value)
            {
                Debug.Assert(!string.IsNullOrEmpty(optionName) && optionName[0] == '-');
                if (argument.StartsWith(optionName + ":", StringComparison.OrdinalIgnoreCase))
                {
                    value = argument.Substring(optionName.Length + 1);
                    return !string.IsNullOrEmpty(value);
                }

                value = null;
                return false;
            }

            var opt = new Options { XunitPath = args[0], UseHtml = true, UseCachedResults = true, TestResultXmlOutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestResults") };
            var index = 1;
            var allGood = true;
            while (index < args.Length)
            {
                var current = args[index];
                if (comparer.Equals(current, "-test64"))
                {
                    opt.Test64 = true;
                    index++;
                }
                else if (comparer.Equals(current, "-testVsi"))
                {
                    opt.TestVsi = true;
                    opt.UseCachedResults = false;
                    index++;
                }
                else if (comparer.Equals(current, "-xml"))
                {
                    opt.UseHtml = false;
                    index++;
                }
                else if (comparer.Equals(current, "-nocache"))
                {
                    opt.UseCachedResults = false;
                    index++;
                }
                else if (isOption(current, "-tfm", out string targetFrameworkMoniker))
                {
                    opt.TargetFrameworkMoniker = targetFrameworkMoniker;
                    index++;
                }
                else if (isOption(current, "-out", out string value))
                {
                    opt.TestResultXmlOutputDirectory = value;
                    index++;
                }
                else if (isOption(current, "-logs", out string logsPath))
                {
                    opt.LogFilesOutputDirectory = logsPath;
                    index++;
                }
                else if (isOption(current, "-secondaryLogs", out string secondaryLogsPath))
                {
                    opt.LogFilesSecondaryOutputDirectory = secondaryLogsPath;
                    index++;
                }
                else if (isOption(current, "-display", out value))
                {
                    if (Enum.TryParse(value, ignoreCase: true, result: out Display display))
                    {
                        opt.Display = display;
                    }
                    else
                    {
                        Console.WriteLine($"{value} is not a valid option for display");
                        allGood = false;
                    }

                    index++;
                }
                else if (isOption(current, "-trait", out value))
                {
                    opt.Trait = value;
                    index++;
                }
                else if (isOption(current, "-notrait", out value))
                {
                    opt.NoTrait = value;
                    index++;
                }
                else if (isOption(current, "-timeout", out value))
                {
                    if (int.TryParse(value, out var minutes))
                    {
                        opt.Timeout = TimeSpan.FromMinutes(minutes);
                    }
                    else
                    {
                        Console.WriteLine($"{value} is not a valid minute value for timeout");
                        allGood = false;
                    }

                    index++;
                }
                else if (isOption(current, "-procdumpPath", out value))
                {
                    opt.ProcDumpDirectory = value;
                    index++;
                }
                else if (comparer.Equals(current, "-useprocdump"))
                {
                    opt.UseProcDump = false;
                    index++;
                }
                else
                {
                    break;
                }
            }

            try
            {
                opt.XunitPath = opt.Test64
                    ? Path.Combine(opt.XunitPath, "xunit.console.exe")
                    : Path.Combine(opt.XunitPath, "xunit.console.x86.exe");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"{opt.XunitPath} is not a valid path: {ex.Message}");
                return null;
            }

            if (!File.Exists(opt.XunitPath))
            {
                Console.WriteLine($"The file '{opt.XunitPath}' does not exist.");
                return null;
            }

            if (opt.UseProcDump && string.IsNullOrEmpty(opt.ProcDumpDirectory))
            {
                Console.WriteLine($"The option 'useprocdump' was specified but 'procdumppath' was not provided");
                return null;
            }

            // If we weren't passed both -logs and -out but just -out, use the same value for -logs too.
            if (opt.LogFilesOutputDirectory == null)
            {
                opt.LogFilesOutputDirectory = opt.TestResultXmlOutputDirectory;
            }

            // If we weren't passed both -secondaryLogs and -logs but just -logs (or -out), use the same value for -secondaryLogs too.
            opt.LogFilesSecondaryOutputDirectory ??= opt.LogFilesOutputDirectory;

            opt.Assemblies = args.Skip(index).ToList();
            return allGood ? opt : null;
        }

        public static void PrintUsage()
        {
            Console.WriteLine("runtests [xunit-console-runner] [-test64] [-xml] [-trait:name1=value1;...] [-notrait:name1=value1;...] [assembly1] [assembly2] [...]");
            Console.WriteLine("Example:");
            Console.WriteLine(@"runtests c:\path-that-contains-xunit.console.exe\ -trait:Feature=Classification Assembly1.dll Assembly2.dll");
        }
    }
}
