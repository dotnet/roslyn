// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#r "./../../Roslyn.Test.Performance.Utilities.dll"

// IsVerbose()
#load "../util/tools_util.csx"
// RunFile()
#load "../util/runner_util.csx"

using System.IO;
using Roslyn.Test.Performance.Utilities;
using static Roslyn.Test.Performance.Utilities.Tools;
using static Roslyn.Test.Performance.Utilities.TestUtilities;

InitUtilitiesFromCsx();
var directoryUtil = new RelativeDirectory();
var logger = new ConsoleAndFileLogger();

// Gather performance metrics and produce csv files
await RunFile(Path.Combine(directoryUtil.MyWorkingDirectory, "..", "runner.csx"));

// Convert the produced consumptionTempResults.xml file to consumptionTempResults.csv file
var elapsedTimeCsvFilePath = Path.Combine(directoryUtil.CPCDirectoryPath, "consumptionTempResults_ElapsedTime.csv");
var result = ConvertConsumptionToCsv(Path.Combine(directoryUtil.CPCDirectoryPath, "consumptionTempResults.xml"), elapsedTimeCsvFilePath, "Duration_TotalElapsedTime", logger);

if (result)
{
    var elapsedTimeViBenchJsonFilePath = GetViBenchJsonFromCsv(elapsedTimeCsvFilePath, null, null, IsVerbose(), logger);
    string jsonFileName = Path.GetFileName(elapsedTimeViBenchJsonFilePath);

    // Move the json file to a file-share
    Log("Copy the json file to the share");
    File.Copy(elapsedTimeViBenchJsonFilePath, $@"\\vcbench-srv4\benchview\uploads\vibench\{jsonFileName}");
}
else
{
    Log("Conversion from Consumption to csv failed.");
}

// Move the traces to mlangfs1 share
UploadTraces(directoryUtil.CPCDirectoryPath, @"\\mlangfs1\public\basoundr\PerfTraces", logger);
