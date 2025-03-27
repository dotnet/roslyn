// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;

using static Roslyn.Test.Performance.Utilities.TestUtilities;

namespace Roslyn.Test.Performance.Utilities
{
    public static class Benchview
    {
        private const string s_sasEnvironmentVar = "BV_UPLOAD_SAS_TOKEN";
        private static readonly string s_scriptDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "tools", "Microsoft.BenchView.JSONFormat", "tools");
        private static readonly string s_outputDirectory = GetCPCDirectoryPath();
        private static readonly string[] s_validSubmissionTypes = ["rolling", "private", "local"];

        private static string s_submissionType;
        private static string s_branch;

        public static string[] ValidSubmissionTypes
        {
            get
            {
                return s_validSubmissionTypes;
            }
        }

        public static bool IsValidSubmissionType(string submissionType)
        {
            return s_validSubmissionTypes.Any(type => type == submissionType);
        }

        public static bool CheckEnvironment()
        {
            Log("Checking for valid environment");

            var sasToken = Environment.GetEnvironmentVariable(s_sasEnvironmentVar);
            if (String.IsNullOrEmpty(sasToken))
            {
                Log($"{s_sasEnvironmentVar} was not defined");
                return false;
            }

            var whereGit = ShellOut("where", "git");
            if (whereGit.Failed)
            {
                Log("git was not found on the PATH");
                return false;
            }

            var wherePy = ShellOut("where", "py");
            if (wherePy.Failed)
            {
                Log("py was not found on the PATH");
                return false;
            }

            if (!Directory.Exists(s_scriptDirectory))
            {
                Log($"BenchView Tools not found at {s_scriptDirectory}");
                return false;
            }

            return true;
        }

        public static void SetConfiguration(string submissionType, string branch)
        {
            s_submissionType = submissionType;
            s_branch = branch;
        }

        public static void UploadBenchviewReport(string submissionName)
        {
            var consumptionXml = Path.Combine(GetCPCDirectoryPath(), "consumptionTempResults.xml");
            UploadBenchviewReport(consumptionXml, submissionName);
        }

        public static void UploadBenchviewReport(string filepath, string submissionName)
        {
            var consumptionXml = Path.Combine(GetCPCDirectoryPath(), "consumptionTempResults.xml");
            var result = ConvertConsumptionToMeasurementJson(filepath);

            if (result)
            {
                var submissionJson = CreateSubmissionJson(s_submissionType, submissionName, s_branch, Path.Combine(s_outputDirectory, "measurement.json"));
                System.Console.Write(System.IO.File.ReadAllText(submissionJson));
                Log("Uploading json to Azure blob storage");
                var uploadPy = Path.Combine(s_scriptDirectory, "upload.py");
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

            var measurementPy = Path.Combine(s_scriptDirectory, "measurement.py");
            var measurementJson = Path.Combine(s_outputDirectory, "measurement.json");
            ShellOutVital("py", $"\"{measurementPy}\" rps \"{source}\" --better desc -o \"{measurementJson}\"");

            return true;
        }

        /// Takes a measurement.json in BenchView's format and generates a submission.json, ready for upload 
        private static string CreateSubmissionJson(string submissionType, string submissionName, string branch, string measurementJsonPath)
        {
            RuntimeSettings.Logger.Log("Creating BenchView submission json");

            var submissionMetadataPy = Path.Combine(s_scriptDirectory, "submission-metadata.py");
            var buildPy = Path.Combine(s_scriptDirectory, "build.py");
            var machinedataPy = Path.Combine(s_scriptDirectory, "machinedata.py");
            var submissionPy = Path.Combine(s_scriptDirectory, "submission.py");

            var submissionMetadataJson = Path.Combine(s_outputDirectory, "submission-metadata.json");
            var buildJson = Path.Combine(s_outputDirectory, "build.json");
            var machinedataJson = Path.Combine(s_outputDirectory, "machinedata.json");

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

            string submissionJson = Path.Combine(s_outputDirectory, "submission.json");

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
