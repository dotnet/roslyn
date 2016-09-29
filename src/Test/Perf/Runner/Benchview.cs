// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Roslyn.Test.Performance.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using static Roslyn.Test.Performance.Utilities.TestUtilities;
using static Roslyn.Test.Performance.Runner.Tools;

namespace Roslyn.Test.Performance.Runner
{
    public static class Benchview 
    {
        private const string s_BenchviewPath = @"\\vcbench-srv4\benchview\uploads\vibench";

        internal static void UploadBenchviewReport()
        {
            // Convert the produced consumptionTempResults.xml file to consumptionTempResults.csv file
            var elapsedTimeCsvFilePath = Path.Combine(GetCPCDirectoryPath(), "consumptionTempResults_ElapsedTime.csv");
            var result = ConvertConsumptionToCsv(Path.Combine(GetCPCDirectoryPath(), "consumptionTempResults.xml"), elapsedTimeCsvFilePath, "Duration_TotalElapsedTime");

            if (result)
            {
                var elapsedTimeViBenchJsonFilePath = GetViBenchJsonFromCsv(elapsedTimeCsvFilePath, null, null);
                string jsonFileName = Path.GetFileName(elapsedTimeViBenchJsonFilePath);

                Log("Copying the json file to the share");
                File.Copy(elapsedTimeViBenchJsonFilePath, Path.Combine(s_BenchviewPath, jsonFileName));
                Log("Done Copying");
            }
            else
            {
                Log("Conversion from Consumption to csv failed.");
            }
        }

        /// Takes a consumptionTempResults file and converts to csv file
        /// Each info contains the {ScenarioName, Metric Key, Metric value}
        private static bool ConvertConsumptionToCsv(string source, string destination, string requiredMetricKey)
        {
            Log("Entering ConvertConsumptionToCsv");
            if (!File.Exists(source))
            {
                Log($"File {source} does not exist");
                return false;
            }

            try
            {
                var result = new List<string>();
                string currentScenarioName = null;

                using (XmlReader xmlReader = XmlReader.Create(source))
                {
                    while (xmlReader.Read())
                    {
                        if ((xmlReader.NodeType == XmlNodeType.Element))
                        {
                            if (xmlReader.Name.Equals("ScenarioResult"))
                            {
                                currentScenarioName = xmlReader.GetAttribute("Name");

                                // These are not test results
                                if (string.Equals(currentScenarioName, "..TestDiagnostics.."))
                                {
                                    currentScenarioName = null;
                                }
                            }
                            else if (currentScenarioName != null && xmlReader.Name.Equals("CounterResult"))
                            {
                                var metricKey = xmlReader.GetAttribute("Name");

                                if (string.Equals(metricKey, requiredMetricKey))
                                {
                                    var metricScale = xmlReader.GetAttribute("Units");
                                    xmlReader.Read();
                                    var metricvalue = xmlReader.Value;
                                    result.Add($"{currentScenarioName}, {metricKey} ({metricScale}), {metricvalue}");
                                }
                            }
                        }
                    }
                }

                File.WriteAllLines(destination, result);
            }
            catch (System.Exception e)
            {
                Log(e.Message);
                Log(e.StackTrace);
                return false;
            }

            return true;
        }

        /// Gets a csv file with metrics and converts them to ViBench supported JSON file
        private static string GetViBenchJsonFromCsv(string compilerTimeCsvFilePath, string execTimeCsvFilePath, string fileSizeCsvFilePath)
        {
            RuntimeSettings.Logger.Log("Convert the csv to JSON using ViBench tool");
            string branch = StdoutFrom("git", "rev-parse --abbrev-ref HEAD");
            string date = FirstLine(StdoutFrom("git", $"show --format=\"%aI\" {branch} --"));
            string hash = FirstLine(StdoutFrom("git", $"show --format=\"%h\" {branch} --"));
            string longHash = FirstLine(StdoutFrom("git", $"show --format=\"%H\" {branch} --"));
            string username = StdoutFrom("whoami");
            string machineName = StdoutFrom("hostname");
            string architecture = System.Environment.Is64BitOperatingSystem ? "x86-64" : "x86";

            // File locations
            string outJson = Path.Combine(GetCPCDirectoryPath(), $"Roslyn-{longHash}.json");

            // ViBenchToJson does not like empty csv files.
            string files = "";
            if (compilerTimeCsvFilePath != null && new FileInfo(compilerTimeCsvFilePath).Length != 0)
            {
                files += $@"compilertime:""{compilerTimeCsvFilePath}""";
            }
            if (execTimeCsvFilePath != null && new FileInfo(execTimeCsvFilePath).Length != 0)
            {
                files += $@"exectime:""{execTimeCsvFilePath}""";
            }
            if (fileSizeCsvFilePath != null && new FileInfo(fileSizeCsvFilePath).Length != 0)
            {
                files += $@"filesize:""{fileSizeCsvFilePath}""";
            }
            string arguments = $@"
    {files}
    jobName:""RoslynPerf-{hash}-{date}""
    jobGroupName:""Roslyn-{branch}""
    jobTypeName:""official""
    branch:""{branch}""
    buildInfoName:""{date}-{branch}-{hash}""
    configName:""Default Configuration""
    machinePoolName:""4-core-windows""
    architectureName:""{architecture}""
    manufacturerName:""unknown-manufacturer""
    microarchName:""unknown-microarch""
    userName:""{username}""
    userAlias:""{username}""
    osInfoName:""Windows""
    machineName:""{machineName}""
    buildNumber:""{date}-{hash}""
    /json:""{outJson}""
";

            arguments = arguments.Replace("\r\n", " ").Replace("\n", "");

            ShellOutVital(Path.Combine(GetCPCDirectoryPath(), "ViBenchToJson.exe"), arguments, workingDirectory: "");

            return outJson;
        }
    }
}
