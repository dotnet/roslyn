// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load "..\util\test_util.csx"
using System.IO;
using System;

InitUtilities();

string FirstLine(string input) {
    return input.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None)[0];
}

string branch = StdoutFrom("git", "rev-parse --abbrev-ref HEAD");
string date = FirstLine(StdoutFrom("git", $"show --format=\"%aI\" {branch} --"));
string hash = FirstLine(StdoutFrom("git", $"show --format=\"%h\" {branch} --"));
string longHash = FirstLine(StdoutFrom("git", $"show --format=\"%H\" {branch} --"));
string username = StdoutFrom("whoami");
string machineName = StdoutFrom("hostname");
string architecture = System.Environment.Is64BitOperatingSystem ? "x86-64" : "x86";

// File locations
string workingDir = Path.Combine(MyWorkingDirectory(), "..", "temp");
string inCompilerTime = Path.Combine(workingDir, "compiler_time.csv");
string inRunTime = Path.Combine(workingDir, "run_time.csv");
string inFileSize = Path.Combine(workingDir, "file_size.csv");
string outJson = Path.Combine(workingDir, $"Roslyn-{longHash}.json");

// ViBenchToJson does not like empty csv files.
string files = "";
if (new FileInfo(inCompilerTime).Length != 0) {
    files += $@"compilertime:""{inCompilerTime}""";
}
if (new FileInfo(inRunTime).Length != 0) {
    files += $@"exectime:""{inRunTime}""";
}
if (new FileInfo(inFileSize).Length != 0) {
    files += $@"filesize:""{inFileSize}""";
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

ShellOutVital(@"\\vcuts-server\tools\ViBenchCsvToJson\ViBenchToJson.exe", arguments);
