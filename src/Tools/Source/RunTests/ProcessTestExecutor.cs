// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using TestExecutor;

namespace RunTests
{
    internal sealed class ProcessTestExecutor
    {
        public static string GetFilterString(WorkItemInfo workItemInfo, Options options)
        {
            var builder = new StringBuilder();
            var filters = workItemInfo.Filters.Values.SelectMany(filter => filter);
            if (filters.Any() || !string.IsNullOrWhiteSpace(options.TestFilter))
            {
                var any = false;
                foreach (var filter in filters)
                {
                    MaybeAddSeparator();
                    builder.Append(filter.GetFilterString());
                }

                if (options.TestFilter is object)
                {
                    MaybeAddSeparator();
                    builder.Append(options.TestFilter);
                }

                void MaybeAddSeparator(char separator = '|')
                {
                    if (any)
                    {
                        builder.Append(separator);
                    }

                    any = true;
                }
            }

            return builder.ToString();
        }

        public static string GetRunSettings(WorkItemInfo workItemInfo, Options options)
        {
            var blameCrashSetting = !options.CollectDumps ? "<CollectDump CollectAlways=\"false\" DumpType=\"full\" />" : string.Empty;

            // The 25 minute timeout in integration tests accounts for the fact that VSIX deployment and/or experimental hive reset and
            // configuration can take significant time (seems to vary from ~10 seconds to ~15 minutes), and the blame
            // functionality cannot separate this configuration overhead from the first test which will eventually run.
            // https://github.com/dotnet/roslyn/issues/59851
            //
            // Helix timeout is 15 minutes as helix jobs fully timeout in 30minutes.  So in order to capture dumps we need the timeout
            // to be 2x shorter than the expected test run time (15min) in case only the last test hangs.
            var timeout = options.UseHelix ? TimeSpan.FromMinutes(15) : TimeSpan.FromMinutes(25);

            var xunitResultsFilePath = GetResultsFilePath(workItemInfo, options, "xml");
            var htmlResultsFilePath = GetResultsFilePath(workItemInfo, options, "html");
            var includeHtmlLogger = options.IncludeHtml ? "True" : "False";

            var runsettingsDocument = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <!-- Configurations that affect the Test Framework -->
  <RunConfiguration>
    <MaxCpuCount>0</MaxCpuCount>
    <TargetPlatform>{options.Architecture}</TargetPlatform>
  </RunConfiguration>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName=""blame"" enabled=""True"">
        <Configuration>
          <CollectDumpOnTestSessionHang TestTimeout=""{timeout.TotalMilliseconds}"" DumpType=""full"" />
          {blameCrashSetting}
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
  <LoggerRunSettings>
    <Loggers>
      <Logger friendlyName=""blame"" enabled=""True"" />
      <Logger friendlyName=""xunit"" enabled=""True"">
        <Configuration>
          <LogFilePath>{xunitResultsFilePath}</LogFilePath>
        </Configuration>
      </Logger>
      <Logger friendlyName=""html"" enabled=""{includeHtmlLogger}"">
        <Configuration>
          <LogFilePath>{htmlResultsFilePath}</LogFilePath>
        </Configuration>
      </Logger>
    </Loggers>
  </LoggerRunSettings>
</RunSettings>";

            return runsettingsDocument;
        }

        private static string GetResultsFilePath(WorkItemInfo workItemInfo, Options options, string suffix = "xml")
        {
            var fileName = $"WorkItem_{workItemInfo.PartitionIndex}_{options.Architecture}_test_results.{suffix}";
            return Path.Combine(options.TestResultsDirectory, fileName);
        }

        public static async Task<TestResult> RunTestAsync(WorkItemInfo workItemInfo, Options options, CancellationToken cancellationToken)
        {
            var result = await RunTestAsyncInternal(workItemInfo, options, isRetry: false, cancellationToken);

            // For integration tests (TestVsi), we make one more attempt to re-run failed tests.
            if (options.Retry && !HasBuiltInRetry(workItemInfo) && !options.IncludeHtml && !result.Succeeded)
            {
                return await RunTestAsyncInternal(workItemInfo, options, isRetry: true, cancellationToken);
            }

            return result;

            static bool HasBuiltInRetry(WorkItemInfo workItemInfo)
            {
                // vs-extension-testing handles test retry internally.
                return workItemInfo.Filters.Keys.Any(key => key.AssemblyName == "Microsoft.VisualStudio.LanguageServices.New.IntegrationTests.dll");
            }
        }

        private static async Task<TestResult> RunTestAsyncInternal(WorkItemInfo workItemInfo, Options options, bool isRetry, CancellationToken cancellationToken)
        {
            try
            {
                var runsettingsContents = GetRunSettings(workItemInfo, options);
                var filterString = GetFilterString(workItemInfo, options);

                var resultsFilePath = GetResultsFilePath(workItemInfo, options);
                var resultsDir = Path.GetDirectoryName(resultsFilePath);
                var htmlResultsFilePath = options.IncludeHtml ? GetResultsFilePath(workItemInfo, options, "html") : null;

                // NOTE: xUnit doesn't always create the log directory
                Directory.CreateDirectory(resultsDir!);

                // Define environment variables for processes started via ProcessRunner.
                var environmentVariables = new Dictionary<string, string>();

                if (isRetry && File.Exists(resultsFilePath))
                {
                    ConsoleUtil.WriteLine("Starting a retry. Tests which failed will run a second time to reduce flakiness.");
                    try
                    {
                        var doc = XDocument.Load(resultsFilePath);
                        foreach (var test in doc.XPathSelectElements("/assemblies/assembly/collection/test[@result='Fail']"))
                        {
                            ConsoleUtil.WriteLine($"  {test.Attribute("name")!.Value}: {test.Attribute("result")!.Value}");
                        }
                    }
                    catch
                    {
                        ConsoleUtil.WriteLine("  ...Failed to identify the list of specific failures.");
                    }

                    // Copy the results file path, since the new xunit run will overwrite it
                    var backupResultsFilePath = Path.ChangeExtension(resultsFilePath, ".old");
                    File.Copy(resultsFilePath, backupResultsFilePath, overwrite: true);

                    // If running the process with this varialbe added, we assume that this file contains 
                    // xml logs from the first attempt.
                    environmentVariables.Add("OutputXmlFilePath", backupResultsFilePath);
                }

                // NOTE: xUnit seems to have an occasional issue creating logs create
                // an empty log just in case, so our runner will still fail.
                File.Create(resultsFilePath).Close();

                Logger.Log($"Running tests in work item {workItemInfo.DisplayName}");
                var testResult = TestPlatformWrapper.RunTests(workItemInfo.Filters.Keys.Select(a => a.AssemblyPath), filterString, runsettingsContents, options.DotnetFilePath);

                Logger.Log($"Test run finished with code {testResult.ExitCode}, ran to completion: {testResult.RanToCompletion}");
                Logger.Log($"Took {testResult.Elapsed}");

                if (testResult.ExitCode != 0)
                {
                    // On occasion we get a non-0 output but no actual data in the result file.  The could happen
                    // if xunit manages to crash when running a unit test (a stack overflow could cause this, for instance).
                    // To avoid losing information, write the process output to the console.  In addition, delete the results
                    // file to avoid issues with any tool attempting to interpret the (potentially malformed) text.
                    var resultData = string.Empty;
                    try
                    {
                        resultData = File.ReadAllText(resultsFilePath).Trim();
                    }
                    catch
                    {
                        // Happens if xunit didn't produce a log file
                    }

                    if (resultData.Length == 0)
                    {
                        // Delete the output file.
                        File.Delete(resultsFilePath);
                        resultsFilePath = null;
                        htmlResultsFilePath = null;
                    }
                }

                return new TestResult(
                    workItemInfo,
                    testResult,
                    resultsFilePath,
                    htmlResultsFilePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to run {workItemInfo.DisplayName} with {options.DotnetFilePath}. {ex}");
            }
        }
    }
}
