// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RunTests.Cache;

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

            var testExecutor = CreateTestExecutor(options);
            var testRunner = new TestRunner(options, testExecutor);
            var start = DateTime.Now;

            Console.WriteLine($"Data Storage: {testExecutor.DataStorage.Name}");
            Console.WriteLine($"Running {options.Assemblies.Count()} test assemblies");

            var orderedList = OrderAssemblyList(options.Assemblies);
            var result = testRunner.RunAllAsync(orderedList, cts.Token).Result;
            var span = DateTime.Now - start;

            foreach (var assemblyPath in options.MissingAssemblies)
            {
                ConsoleUtil.WriteLine(ConsoleColor.Red, $"The file '{assemblyPath}' does not exist, is an invalid file name, or you do not have sufficient permissions to read the specified file.");
            }

            Logger.Finish();

            if (!result)
            {
                ConsoleUtil.WriteLine(ConsoleColor.Red, "Test failures encountered: {0}", span);
                return 1;
            }

            Console.WriteLine("All tests passed: {0}", span);
            return options.MissingAssemblies.Any() ? 1 : 0;
        }

        private static ITestExecutor CreateTestExecutor(Options options)
        {
            var processTestExecutor = new ProcessTestExecutor(options);
            if (!options.UseCachedResults)
            {
                return processTestExecutor;
            }

            // The web caching layer is still being worked on.  For now want to limit it to Roslyn developers
            // and Jenkins runs by default until we work on this a bit more.  Anyone reading this who wants
            // to try it out should feel free to opt into this. 
            IDataStorage dataStorage = new LocalDataStorage();
            if (StringComparer.OrdinalIgnoreCase.Equals("REDMOND", Environment.UserDomainName) || Constants.IsJenkinsRun)
            {
                dataStorage = new WebDataStorage();
            }

            return new CachingTestExecutor(options, processTestExecutor, dataStorage);
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