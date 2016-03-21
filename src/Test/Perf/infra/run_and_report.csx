// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#load "..\util\test_util.csx"
#load "..\util\runner_util.csx"

using System.IO;

InitUtilities();

// Gather performance metrics and produce csv files
await RunFile(Path.Combine(MyWorkingDirectory(), "..", "runner.csx"));

// Transform those csvs into a .json file
var state =  await RunFile(Path.Combine(MyWorkingDirectory(), "transform_csv.csx"));
var outJsonPath = (string) state.GetVariable("outJson").Value;
string jsonFileName = Path.GetFileName(outJsonPath);

// Move the json file to a file-share
File.Copy(outJsonPath, $@"\\vcbench-srv4\benchview\uploads\vibench\{jsonFileName}");
