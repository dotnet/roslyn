// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#load "..\util\test_util.csx"
#load "..\util\runner_util.csx"

using System.IO;

InitUtilities();

// Gather performance metrics and produce csv files
await RunFile(Path.Combine(MyWorkingDirectory(), "..", "runner.csx"));

// Convert the produced consumptionTempResults.xml file to consumptionTempResults.csv file
var elapsedTimeCsvFilePath = Path.Combine(GetCPCDirectoryPath(), "consumptionTempResults_ElapsedTime.csv");
var result = ConvertConsumptionToCsv(Path.Combine(MyWorkingDirectory(), "..", "consumptionTempResults.xml"), elapsedTimeCsvFilePath, "Duration_TotalElapsedTime");

if (!result)
{
    return;
}

var elapsedTimeViBenchJsonFilePath = GetViBenchJsonFromCsv(elapsedTimeCsvFilePath, null, null);
string jsonFileName = Path.GetFileName(elapsedTimeViBenchJsonFilePath);

// Move the json file to a file-share
File.Copy(elapsedTimeViBenchJsonFilePath, $@"\\vcbench-srv4\benchview\uploads\vibench\{jsonFileName}");

// Move the traces to mlangfs1 share
UploadTraces(GetCPCDirectoryPath(), @"\\mlangfs1\public\basoundr\PerfTraces");