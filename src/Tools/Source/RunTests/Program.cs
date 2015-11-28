// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    internal sealed class Program
    {
        internal static int Main(string[] args)
        {
            var options = Options.Parse(args);
            if (options == null)
            {
                Options.PrintUsage();
                return 1;
            }

            // Setup cancellation for ctrl-c key presses
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += delegate
            {
                cts.Cancel();
            };

            var testRunner = new TestRunner(new ProcessTestExecutor(options));
            var start = DateTime.Now;

            Console.WriteLine("Running {0} test assemblies", options.Assemblies.Count());

            var orderedList = OrderAssemblyList(options.Assemblies);
            var result = testRunner.RunAllAsync(orderedList, cts.Token).Result;
            var span = DateTime.Now - start;

            foreach (var assemblyPath in options.MissingAssemblies)
            {
                ConsoleUtil.WriteLine(ConsoleColor.Red, $"The file '{assemblyPath}' does not exist, is an invalid file name, or you do not have sufficient permissions to read the specified file.");
            }

            if (!result)
            {
                ConsoleUtil.WriteLine(ConsoleColor.Red, "Test failures encountered: {0}", span);
                return 1;
            }

            Console.WriteLine("All tests passed: {0}", span);
            return options.MissingAssemblies.Any() ? 1 : 0;
        }

        /// <summary>
        /// Order the assembly list so that the largest assemblies come first.  This
        /// is not ideal as the largest assembly does not necessarily take the most time.
        /// </summary>
        /// <param name="list"></param>
        private static IOrderedEnumerable<string> OrderAssemblyList(IEnumerable<string> list) =>
            list.OrderByDescending((assemblyName) => new FileInfo(assemblyName).Length);
    }
}