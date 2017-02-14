﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;

using Roslyn.Test.Performance.Utilities;
using static Roslyn.Test.Performance.Utilities.TestUtilities;

namespace Roslyn.Test.Performance.Runner
{
    public static class Benchview
    {
        private const string s_sasEnvironmentVar = "BV_UPLOAD_SAS_TOKEN";
        private static readonly string s_scriptDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "tools", "Microsoft.BenchView.JSONFormat", "tools");
        private static readonly string s_outputDirectory = GetCPCDirectoryPath();
        private static readonly string[] s_validSubmissionTypes = new string[] { "rolling", "private", "local" };

        public static string[] ValidSubmissionTypes
        {
            get
            {
                return s_validSubmissionTypes;
            }
        }

        internal static bool IsValidSubmissionType(string submissionType)
        {
            return s_validSubmissionTypes.Any(type => type == submissionType);
        }

        internal static bool CheckEnvironment()
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

        internal static void UploadBenchviewReport(string submissionType, string submissionName, string branch)
        {
            var consumptionXml = Path.Combine(GetCPCDirectoryPath(), "consumptionTempResults.xml");
            var result = ConvertConsumptionToMeasurementJson(consumptionXml);

            if (result)
            {
                var submissionJson = CreateSubmissionJson(submissionType, submissionName, branch, Path.Combine(s_outputDirectory, "measurement.json"));

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

            Log("START_XML");
            Log(File.ReadAllText(source));
            Log("END_XML");

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
