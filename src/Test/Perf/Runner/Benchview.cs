// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

using Roslyn.Test.Performance.Utilities;
using static Roslyn.Test.Performance.Utilities.TestUtilities;
using static Roslyn.Test.Performance.Runner.Tools;

namespace Roslyn.Test.Performance.Runner
{
    public static class Benchview 
    {
        const string sasEnvVar = "BV_UPLOAD_SAS_TOKEN";
        static string scriptDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Microsoft.BenchView.JSONFormat", "tools");
        static string outputDir = GetCPCDirectoryPath();
        static string[] validSubmissionTypes = new string[] { "rolling", "private", "local" };

        public static string[] ValidSubmissionTypes
        {
            get
            {
                return validSubmissionTypes;
            }
        }

        internal static bool IsValidSubmissionType(string submissionType)
        {
            foreach(var type in validSubmissionTypes)
            {
                if(submissionType == type)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void CheckEnvironment()
        {
            var sasToken = Environment.GetEnvironmentVariable(sasEnvVar);
            if (String.IsNullOrEmpty(sasToken))
            {
                throw new Exception($"{sasEnvVar} was not defined");
            }

            var whereGit = ShellOut("where", "git");
            if (whereGit.Failed)
            {
                throw new Exception("git was not found on the PATH");
            }

            var wherePy = ShellOut("where", "py");
            if (wherePy.Failed)
            {
                throw new Exception("py was not found on the PATH");
            }

            if (!Directory.Exists(scriptDir))
            {
                throw new Exception($"BenchView Tools not found at {scriptDir}");
            }
        }

        internal static void UploadBenchviewReport(string submissionType, string submissionName, string branch)
        {
            var consumptionXml = Path.Combine(GetCPCDirectoryPath(), "consumptionTempResults.xml");
            var result = ConvertConsumptionToMeasurementJson(consumptionXml);

            if (result)
            {
                var submissionJson = CreateSubmissionJson(submissionType, submissionName, branch, Path.Combine(outputDir, "measurement.json"));

                Log("Uploading json to Azure blob storage");
                var uploadPy = Path.Combine(scriptDir, "upload.py");
                ShellOutVital("py", $"\"{uploadPy}\" \"{submissionJson}\" --container roslyn");
                Log("Done uploading");
            }
            else
            {
                Log("Conversion from Consumption to json failed.");
            }
        }

        /// Takes a consumption xml file and converts to measurement json file
        private static bool ConvertConsumptionToMeasurementJson(string source)
        {
            Log("Converting Consumption format to BenchView measurement json");
            if (!File.Exists(source))
            {
                Log($"File {source} does not exist");
                return false;
            }

            var measurementPy = Path.Combine(scriptDir, "measurement.py");
            var measurementJson = Path.Combine(outputDir, "measurement.json");
            ShellOutVital("py", $"\"{measurementPy}\" rps \"{source}\" --better desc -o \"{measurementJson}\"");

            return true;
        }

        /// Takes a measurement.json in BenchView's format and generates a submission.json, ready for upload 
        private static string CreateSubmissionJson(string submissionType, string submissionName, string branch, string measurementJsonPath)
        {
            RuntimeSettings.Logger.Log("Creating BenchView submission json");

            var submissionMetadataPy = Path.Combine(scriptDir, "submission-metadata.py");
            var buildPy = Path.Combine(scriptDir, "build.py");
            var machinedataPy = Path.Combine(scriptDir, "machinedata.py");
            var submissionPy = Path.Combine(scriptDir, "submission.py");

            var submissionMetadataJson = Path.Combine(outputDir, "submission-metadata.json");
            var buildJson = Path.Combine(outputDir, "build.json");
            var machinedataJson = Path.Combine(outputDir, "machinedata.json");

            string hash = StdoutFrom("git", "rev-parse HEAD");
            if (string.IsNullOrWhiteSpace(submissionName))
            {
                if (submissionType == "rolling")
                {
                    submissionName = $"roslyn {submissionType} {branch} {hash}";
                }
                else
                {
                    throw new Exception($"submissionName was not provided, but submission type is {submissionType}");
                }
            }

            ShellOutVital("py", $"\"{submissionMetadataPy}\" --name \"{submissionName}\" --user-email dotnet-bot@microsoft.com -o \"{submissionMetadataJson}\"");
            ShellOutVital("py", $"\"{buildPy}\" git --type {submissionType} --branch \"{branch}\" -o \"{buildJson}\"");
            ShellOutVital("py", $"\"{machinedataPy}\" -o \"{machinedataJson}\"");

            string submissionJson = Path.Combine(outputDir, "submission.json");

#if DEBUG
            string configuration = "Debug";
#else
            string configuration = "Release";
#endif

            string arguments = $@"
""{submissionPy}""
 {measurementJsonPath}
 --metadata ""{submissionMetadataJson}""
 --build ""{buildJson}""
 --machine-data ""{machinedataJson}""
 --group ""roslyn""
 --type {submissionType}
 --config-name {configuration}
 --config configuration {configuration}
 --architecture amd64
 --machinepool ""ml-perf""
 -o ""{submissionJson}""
";

            arguments = arguments.Replace("\r\n", " ").Replace("\n", "");

            ShellOutVital("py", arguments);

            return submissionJson;
        }
    }
}
