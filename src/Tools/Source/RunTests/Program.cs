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
using Newtonsoft.Json.Linq;
using RestSharp;

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

            return RunCore(options, cts.Token).GetAwaiter().GetResult();
        }

        private static async Task<int> RunCore(Options options, CancellationToken cancellationToken)
        {
            var testExecutor = CreateTestExecutor(options);
            var testRunner = new TestRunner(options, testExecutor);
            var start = DateTime.Now;

            Console.WriteLine($"Data Storage: {testExecutor.DataStorage.Name}");
            Console.WriteLine($"Running {options.Assemblies.Count()} test assemblies");

            var orderedList = OrderAssemblyList(options.Assemblies);
            var result = await testRunner.RunAllAsync(orderedList, cancellationToken).ConfigureAwait(true);
            var ellapsed = DateTime.Now - start;

            foreach (var assemblyPath in options.MissingAssemblies)
            {
                ConsoleUtil.WriteLine(ConsoleColor.Red, $"The file '{assemblyPath}' does not exist, is an invalid file name, or you do not have sufficient permissions to read the specified file.");
            }

            Logger.Finish();

            if (CanUseWebStorage())
            {
                await SendRunStats(options, testExecutor.DataStorage, ellapsed, result, cancellationToken).ConfigureAwait(true);
            }

            if (!result.Succeeded)
            {
                ConsoleUtil.WriteLine(ConsoleColor.Red, $"Test failures encountered: {ellapsed}");
                return 1;
            }

            Console.WriteLine($"All tests passed: {ellapsed}");
            return options.MissingAssemblies.Any() ? 1 : 0;
        }

        private static bool CanUseWebStorage()
        {
            // The web caching layer is still being worked on.  For now want to limit it to Roslyn developers
            // and Jenkins runs by default until we work on this a bit more.  Anyone reading this who wants
            // to try it out should feel free to opt into this. 
            return 
                StringComparer.OrdinalIgnoreCase.Equals("REDMOND", Environment.UserDomainName) || 
                Constants.IsJenkinsRun;
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
            if (CanUseWebStorage())
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
        private static IOrderedEnumerable<string> OrderAssemblyList(IEnumerable<string> list)
        {
            return list.OrderByDescending((assemblyName) => new FileInfo(assemblyName).Length);
        }

        private static async Task SendRunStats(Options options, IDataStorage dataStorage, TimeSpan ellapsed, RunAllResult result, CancellationToken cancellationToken)
        {
            var obj = new JObject();
            obj["Cache"] = dataStorage.Name;
            obj["EllapsedSeconds"] = ellapsed.TotalSeconds;
            obj["IsJenkins"] = Constants.IsJenkinsRun;
            obj["Is32Bit"] = !options.Test64;
            obj["AssemblyCount"] = options.Assemblies.Count;
            obj["CacheCount"] = result.CacheCount;
            obj["Succeeded"] = result.Succeeded;

            var request = new RestRequest("api/testrun", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("text/json", obj.ToString(), ParameterType.RequestBody);

            try
            {
                var client = new RestClient(Constants.DashboardUriString);
                var response = await client.ExecuteTaskAsync(request);
                if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    Logger.Log($"Unable to send results: {response.ErrorMessage}");
                }
            }
            catch
            {
                Logger.Log("Unable to send results");
            }
        }
    }
}