// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#load "test_util.csx"

#r "..\infra\bin\Microsoft.CodeAnalysis.Scripting.dll"
#r "..\infra\bin\Microsoft.CodeAnalysis.CSharp.Scripting.dll"

using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

/// Finds all csi files in a directory recursively, ignoring those that
/// are in the "skip" set.
IEnumerable<string> AllCsiRecursive(string start, HashSet<string> skip)
{
    IEnumerable<string> childFiles =
        from fileName in Directory.EnumerateFiles(start.ToString(), "*.csx")
        select fileName;
    IEnumerable<string> grandChildren =
        from childDir in Directory.EnumerateDirectories(start)
        where !skip.Contains(childDir)
        from fileName in AllCsiRecursive(childDir, skip)
        select fileName;
    return childFiles.Concat(grandChildren).Where((s) => !skip.Contains(s));
}

/// Runs the script at fileName and returns a task containing the
/// state of the script.
async Task<ScriptState<object>> RunFile(string fileName)
{
    var scriptOptions = ScriptOptions.Default.WithFilePath(fileName);
    var text = File.ReadAllText(fileName);
    var prelude = "System.Collections.Generic.List<string> Args = null;";
    var state = await CSharpScript.RunAsync(prelude);
    var args = state.GetVariable("Args");
    args.Value = Args;
    return await state.ContinueWithAsync<object>(text, scriptOptions);
}

/// Gets all csx file recursively in a given directory
IEnumerable<string> GetAllCsxRecursive(string directoryName)
{
    foreach (var fileName in Directory.EnumerateFiles(directoryName, "*.csx"))
    {
        yield return fileName;
    }


    foreach (var childDir in Directory.EnumerateDirectories(directoryName))
    {
        foreach (var fileName in GetAllCsxRecursive(childDir))
        {
            yield return fileName;
        }
    }
}

/// Takes a consumptionTempResults file and converts to csv file
/// Each info contains the <ScenarioName, Metric Key, Metric value>
bool ConvertConsumptionToCsv(string source, string destination, string requiredMetricKey)
{
    Log("Entering ConvertConsumptionToCsv");
    if (!File.Exists(source))
    {
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
    catch(System.Exception e)
    {
        System.Console.WriteLine(e.Message);
        System.Console.WriteLine(e.StackTrace);
        return false;
    }

    return true;
}

/// Gets a csv file with metrics and converts them to ViBench supported JSON file
string GetViBenchJsonFromCsv(string compilerTimeCsvFilePath, string execTimeCsvFilePath, string fileSizeCsvFilePath)
{
    Log("Convert the csv to JSON using ViBench tool");
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
    if (compilerTimeCsvFilePath != null && new FileInfo(compilerTimeCsvFilePath).Length != 0) {
        files += $@"compilertime:""{compilerTimeCsvFilePath}""";
    }
    if (execTimeCsvFilePath != null && new FileInfo(execTimeCsvFilePath).Length != 0) {
        files += $@"exectime:""{execTimeCsvFilePath}""";
    }
    if (fileSizeCsvFilePath != null && new FileInfo(fileSizeCsvFilePath).Length != 0) {
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

    ShellOutVital(@"\\mlangfs1\public\basoundr\vibenchcsv2json\ViBenchToJson.exe", arguments);
    
    return outJson;
}

string FirstLine(string input) {
    return input.Split(new[] {"\r\n", "\r", "\n"}, System.StringSplitOptions.None)[0];
}

void UploadTraces(string sourceFolderPath, string destinationFolderPath)
{
    Log("Uploading traces");
    if (Directory.Exists(sourceFolderPath))
    {
        // Get the latest written databackup
        var directoryToUpload = new DirectoryInfo(sourceFolderPath).GetDirectories("DataBackup*").OrderByDescending(d=>d.LastWriteTimeUtc).FirstOrDefault();
        if (directoryToUpload == null)
        {
            Log($"There are no trace directory starting with DataBackup in {sourceFolderPath}");
            return;
        }

        var destination = Path.Combine(destinationFolderPath, directoryToUpload.Name);
        CopyDirectory(directoryToUpload.FullName, destination);
    }
    else
    {
        Log($"sourceFolderPath: {sourceFolderPath} does not exist");
    }
}

