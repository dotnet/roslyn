// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

        public string XunitPath { get; set; }

        internal static Options Parse(string[] args)
        {
            if (args == null || args.Any(a => a == null) || args.Length < 2)
            {
                return null;
            }

            var opt = new Options { XunitPath = args[0], UseHtml = true, UseCachedResults = true };
            var index = 1;
            var allGood = true;
            var comp = StringComparer.OrdinalIgnoreCase;
            while (index < args.Length)
            {
                const string optionTrait = "-trait:";
                const string optionNoTrait = "-notrait:";
                const string optionDisplay = "-display:";

                var current = args[index];
                if (comp.Equals(current, "-test64"))
                {
                    opt.Test64 = true;
                    index++;
                }
                else if (comp.Equals(current, "-xml"))
                {
                    opt.UseHtml = false;
                    index++;
                }
                else if (comp.Equals(current, "-nocache"))
                {
                    opt.UseCachedResults = false;
                    index++;
                }
                else if (current.StartsWith(optionDisplay, StringComparison.OrdinalIgnoreCase))
                {
                    Display display;
                    var arg = current.Substring(optionDisplay.Length);
                    if (Enum.TryParse(arg, ignoreCase: true, result: out display))
                    {
                        opt.Display = display;
                    }
                    else
                    {
                        Console.WriteLine($"{arg} is not a valid option for display");
                        allGood = false;
                    }

                    index++;
                }
                else if (current.StartsWith(optionTrait, StringComparison.OrdinalIgnoreCase))
                {
                    opt.Trait = current.Substring(optionTrait.Length);
                    index++;
                }
                else if (current.StartsWith(optionNoTrait, StringComparison.OrdinalIgnoreCase))
                {
                    opt.NoTrait = current.Substring(optionNoTrait.Length);
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
