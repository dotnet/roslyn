// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RunTests
{
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
        public bool Display { get; set; }

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

        public List<string> MissingAssemblies { get; set; }

        public string XunitPath { get; set; }

        internal static Options Parse(string[] args)
        {
            if (args == null || args.Any(a => a == null) || args.Length < 2)
            {
                return null;
            }

            var opt = new Options { XunitPath = args[0], UseHtml = true, UseCachedResults = true };
            int index = 1;

            var comp = StringComparer.OrdinalIgnoreCase;
            while (index < args.Length)
            {
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
                else if (comp.Equals(current, "-display"))
                {
                    opt.Display = true;
                    index++;
                }
                else if (current.Length > 7 && current.StartsWith("-trait:", StringComparison.OrdinalIgnoreCase))
                {
                    opt.Trait = current.Substring(7);
                    index++;
                }
                else if (current.Length > 9 && current.StartsWith("-notrait:", StringComparison.OrdinalIgnoreCase))
                {
                    opt.NoTrait = current.Substring(9);
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

            opt.Assemblies = new List<string>();
            opt.MissingAssemblies = new List<string>();
            var assemblyArgs = args.Skip(index).ToArray();

            if (!assemblyArgs.Any())
            {
                Console.WriteLine("No test assemblies specified.");
                return null;
            }

            foreach (var assemblyPath in assemblyArgs)
            {
                if (File.Exists(assemblyPath))
                {
                    opt.Assemblies.Add(assemblyPath);
                    continue;
                }
                opt.MissingAssemblies.Add(assemblyPath);
            }

            return opt;
        }

        public static void PrintUsage()
        {
            Console.WriteLine("runtests [xunit-console-runner] [-test64] [-xml] [-trait:name1=value1;...] [-notrait:name1=value1;...] [assembly1] [assembly2] [...]");
            Console.WriteLine("Example:");
            Console.WriteLine(@"runtests c:\path-that-contains-xunit.console.exe\ -trait:Feature=Classification Assembly1.dll Assembly2.dll");
        }
    }
}
