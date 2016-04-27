using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using static Roslyn.Test.Performance.Utilities.TestUtilities;

namespace Roslyn.Test.Performance.Utilities
{
    public class Tools
    {
        /// Takes a consumptionTempResults file and converts to csv file
        /// Each info contains the <ScenarioName, Metric Key, Metric value>
        public static bool ConvertConsumptionToCsv(string source, string destination, string requiredMetricKey, ILogger logger)
        {
            logger.Log("Entering ConvertConsumptionToCsv");
            if (!File.Exists(source))
            {
                logger.Log($"File {source} does not exist");
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
                System.Console.WriteLine(e.Message);
                System.Console.WriteLine(e.StackTrace);
                return false;
            }

            return true;
        }

        /// Gets a csv file with metrics and converts them to ViBench supported JSON file
        public static string GetViBenchJsonFromCsv(string compilerTimeCsvFilePath, string execTimeCsvFilePath, string fileSizeCsvFilePath, bool verbose, ILogger logger)
        {
            logger.Log("Convert the csv to JSON using ViBench tool");
            string branch = StdoutFrom("git", verbose, logger, "rev-parse --abbrev-ref HEAD");
            string date = FirstLine(StdoutFrom("git", verbose, logger, $"show --format=\"%aI\" {branch} --"));
            string hash = FirstLine(StdoutFrom("git", verbose, logger, $"show --format=\"%h\" {branch} --"));
            string longHash = FirstLine(StdoutFrom("git", verbose, logger, $"show --format=\"%H\" {branch} --"));
            string username = StdoutFrom("whoami", verbose, logger);
            string machineName = StdoutFrom("hostname", verbose, logger);
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

            ShellOutVital(Path.Combine(GetCPCDirectoryPath(), "ViBenchToJson.exe"), arguments, verbose, logger);

            return outJson;
        }

        public static string FirstLine(string input)
        {
            return input.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None)[0];
        }

        public static void UploadTraces(string sourceFolderPath, string destinationFolderPath, ILogger logger)
        {
            logger.Log("Uploading traces");
            if (Directory.Exists(sourceFolderPath))
            {
                var directoriesToUpload = new DirectoryInfo(sourceFolderPath).GetDirectories("DataBackup*");
                if (directoriesToUpload.Count() == 0)
                {
                    logger.Log($"There are no trace directory starting with DataBackup in {sourceFolderPath}");
                    return;
                }

                var perfResultDestinationFolderName = string.Format("PerfResults-{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now);

                var destination = Path.Combine(destinationFolderPath, perfResultDestinationFolderName);
                foreach (var directoryToUpload in directoriesToUpload)
                {
                    var destinationDataBackupDirectory = Path.Combine(destination, directoryToUpload.Name);
                    if (Directory.Exists(destinationDataBackupDirectory))
                    {
                        Directory.CreateDirectory(destinationDataBackupDirectory);
                    }

                    CopyDirectory(directoryToUpload.FullName, logger, destinationDataBackupDirectory);
                }

                foreach (var file in new DirectoryInfo(sourceFolderPath).GetFiles().Where(f => f.Name.StartsWith("ConsumptionTemp", StringComparison.OrdinalIgnoreCase) || f.Name.StartsWith("Roslyn-", StringComparison.OrdinalIgnoreCase)))
                {
                    File.Copy(file.FullName, Path.Combine(destination, file.Name));
                }
            }
            else
            {
                logger.Log($"sourceFolderPath: {sourceFolderPath} does not exist");
            }
        }

        public static void CopyDirectory(string source, ILogger logger, string destination, string argument = @"/mir")
        {
            var result = ShellOut("Robocopy", $"{argument} {source} {destination}", verbose: true, logger: logger);

            // Robocopy has a success exit code from 0 - 7
            if (result.Code > 7)
            {
                throw new IOException($"Failed to copy \"{source}\" to \"{destination}\".");
            }
        }

    }
}
